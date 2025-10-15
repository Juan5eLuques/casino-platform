using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// Servicio unificado de wallet que usa Player.WalletBalance como source of truth
/// Maneja TANTO transacciones de gateway (BET, WIN, ROLLBACK) como de backoffice (TRANSFER, MINT, DEPOSIT, etc.)
/// Registra TODAS las transacciones en WalletTransactions con TransactionType correcto según el contexto
/// Mantiene compatibilidad con Ledger como registro secundario
/// CUMPLE con el requisito de unificación: una sola tabla Players + WalletTransactions para ambos sistemas
/// </summary>
public class UnifiedWalletService : IWalletService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<UnifiedWalletService> _logger;

    public UnifiedWalletService(CasinoDbContext context, ILogger<UnifiedWalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Determina el TransactionType basado en el reason/contexto
    /// </summary>
    private static TransactionType DetermineTransactionType(string reason)
    {
        return reason?.ToUpper() switch
        {
            "BET" => TransactionType.BET,
            "WIN" => TransactionType.WIN,
            "DEPOSIT" => TransactionType.DEPOSIT,
            "WITHDRAWAL" => TransactionType.WITHDRAWAL,
            "TRANSFER" => TransactionType.TRANSFER,
            "BONUS" => TransactionType.BONUS,
            "ADJUSTMENT" => TransactionType.ADJUSTMENT,
            "MINT" => TransactionType.MINT,
            _ => TransactionType.BET // Default para compatibilidad
        };
    }

    /// <summary>
    /// Determina el LedgerReason basado en el reason/contexto
    /// </summary>
    private static LedgerReason DetermineLedgerReason(string reason)
    {
        return reason?.ToUpper() switch
        {
            "BET" => LedgerReason.BET,
            "WIN" => LedgerReason.WIN,
            "BONUS" => LedgerReason.BONUS,
            "DEPOSIT" => LedgerReason.ADMIN_GRANT, // Mapear a ADMIN_GRANT
            "WITHDRAWAL" => LedgerReason.ADMIN_DEBIT, // Mapear a ADMIN_DEBIT
            "TRANSFER" => LedgerReason.ADJUST, // Mapear a ADJUST
            "ADJUSTMENT" => LedgerReason.ADJUST,
            "MINT" => LedgerReason.ADMIN_GRANT, // Mapear a ADMIN_GRANT
            _ => LedgerReason.BET // Default para compatibilidad
        };
    }

    public async Task<WalletBalanceResponse> GetBalanceAsync(WalletBalanceRequest request)
    {
        _logger.LogInformation("Getting balance for player: {PlayerId}", request.PlayerId);

        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

        if (player == null)
        {
            throw new InvalidOperationException($"Player {request.PlayerId} not found");
        }

        // Retornar Player.WalletBalance como source of truth
        return new WalletBalanceResponse(player.WalletBalance);
    }

    public async Task<WalletOperationResponse> DebitAsync(WalletDebitRequest request)
    {
        _logger.LogInformation("Debiting wallet - Player: {PlayerId}, Amount: {Amount}, ExternalRef: {ExternalRef}", 
            request.PlayerId, request.Amount, request.ExternalRef);

        // Convertir de centavos (long) a decimal
        decimal amountDecimal = request.Amount / 100.0m;

        // 1. Verificar idempotencia por ExternalRef en WalletTransactions
        if (!string.IsNullOrEmpty(request.ExternalRef))
        {
            var existingTransaction = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.ExternalRef);

            if (existingTransaction != null)
            {
                _logger.LogInformation("Transaction already processed (idempotent): {ExternalRef}", request.ExternalRef);
                
                var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
                return new WalletOperationResponse(
                    true,
                    existingTransaction.Id,
                    "Transaction already processed",
                    player?.WalletBalance ?? 0,
                    null
                );
            }
        }

        // 2. Comenzar transacción DB con SERIALIZABLE
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // 3. Obtener y bloquear player
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

            if (player == null)
            {
                throw new InvalidOperationException($"Player {request.PlayerId} not found");
            }

            // 4. Capturar balance ANTES
            var previousBalance = player.WalletBalance;

            // 5. Validar saldo suficiente
            if (player.WalletBalance < amountDecimal)
            {
                throw new InvalidOperationException(
                    $"Insufficient balance. Required: {amountDecimal}, Available: {player.WalletBalance}"
                );
            }

            // 6. Actualizar balance (DÉBITO)
            player.WalletBalance -= amountDecimal;
            var newBalance = player.WalletBalance;

            // 7. Determinar TransactionType según el contexto
            var transactionType = DetermineTransactionType(request.Reason);
            var ledgerReason = DetermineLedgerReason(request.Reason);

            // 8. Crear registro en WalletTransactions
            var walletTransaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                BrandId = player.BrandId,
                FromUserId = player.Id,
                FromUserType = "PLAYER",
                ToUserId = Guid.Empty, // Placeholder para "La casa"
                ToUserType = "HOUSE",
                Amount = amountDecimal,
                TransactionType = transactionType,
                PreviousBalanceFrom = previousBalance,
                NewBalanceFrom = newBalance,
                PreviousBalanceTo = 0,
                NewBalanceTo = 0,
                Description = $"{request.Reason} - round {request.GameRoundId ?? "N/A"}",
                CreatedByUserId = player.Id,
                CreatedByRole = "PLAYER",
                IdempotencyKey = request.ExternalRef ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(walletTransaction);

            // 9. Crear registro en Ledger (compatibilidad)
            var ledgerEntry = new Ledger
            {
                BrandId = player.BrandId,
                PlayerId = player.Id,
                RoundId = string.IsNullOrEmpty(request.GameRoundId) ? null : Guid.TryParse(request.GameRoundId, out var roundGuid) ? roundGuid : null,
                DeltaBigint = -request.Amount, // Negativo para débito (en centavos)
                Reason = ledgerReason,
                GameCode = null,
                Provider = "unified",
                ExternalRef = request.ExternalRef,
                Meta = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);

            // 10. Guardar cambios
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Debit successful - Player: {PlayerId}, Amount: {Amount}, Type: {Type}, NewBalance: {Balance}", 
                player.Id, amountDecimal, transactionType, newBalance);

            return new WalletOperationResponse(
                true,
                walletTransaction.Id,
                "Debit successful",
                newBalance,
                null
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error debiting wallet - Player: {PlayerId}", request.PlayerId);
            throw;
        }
    }

    public async Task<WalletOperationResponse> CreditAsync(WalletCreditRequest request)
    {
        _logger.LogInformation("Crediting wallet - Player: {PlayerId}, Amount: {Amount}, ExternalRef: {ExternalRef}", 
            request.PlayerId, request.Amount, request.ExternalRef);

        // Convertir de centavos a decimal
        decimal amountDecimal = request.Amount / 100.0m;

        // 1. Verificar idempotencia
        if (!string.IsNullOrEmpty(request.ExternalRef))
        {
            var existingTransaction = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.ExternalRef);

            if (existingTransaction != null)
            {
                _logger.LogInformation("Transaction already processed (idempotent): {ExternalRef}", request.ExternalRef);
                
                var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
                return new WalletOperationResponse(
                    true,
                    existingTransaction.Id,
                    "Transaction already processed",
                    player?.WalletBalance ?? 0,
                    null
                );
            }
        }

        // 2. Transacción DB
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // 3. Obtener y bloquear player
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId);

            if (player == null)
            {
                throw new InvalidOperationException($"Player {request.PlayerId} not found");
            }

            // 4. Capturar balance ANTES
            var previousBalance = player.WalletBalance;

            // 5. Actualizar balance (CRÉDITO)
            player.WalletBalance += amountDecimal;
            var newBalance = player.WalletBalance;

            // 6. Determinar TransactionType según el contexto
            var transactionType = DetermineTransactionType(request.Reason);
            var ledgerReason = DetermineLedgerReason(request.Reason);

            // 7. Crear registro en WalletTransactions
            var walletTransaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                BrandId = player.BrandId,
                FromUserId = Guid.Empty, // "La casa"
                FromUserType = "HOUSE",
                ToUserId = player.Id,
                ToUserType = "PLAYER",
                Amount = amountDecimal,
                TransactionType = transactionType,
                PreviousBalanceFrom = 0,
                NewBalanceFrom = 0,
                PreviousBalanceTo = previousBalance,
                NewBalanceTo = newBalance,
                Description = $"{request.Reason} - round {request.GameRoundId ?? "N/A"}",
                CreatedByUserId = player.Id,
                CreatedByRole = "PLAYER",
                IdempotencyKey = request.ExternalRef ?? Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(walletTransaction);

            // 8. Crear registro en Ledger
            var ledgerEntry = new Ledger
            {
                BrandId = player.BrandId,
                PlayerId = player.Id,
                RoundId = string.IsNullOrEmpty(request.GameRoundId) ? null : Guid.TryParse(request.GameRoundId, out var roundGuid) ? roundGuid : null,
                DeltaBigint = request.Amount, // Positivo para crédito (en centavos)
                Reason = ledgerReason,
                GameCode = null,
                Provider = "unified",
                ExternalRef = request.ExternalRef,
                Meta = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);

            // 9. Guardar y commit
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Credit successful - Player: {PlayerId}, Amount: {Amount}, Type: {Type}, NewBalance: {Balance}", 
                player.Id, amountDecimal, transactionType, newBalance);

            return new WalletOperationResponse(
                true,
                walletTransaction.Id,
                "Credit successful",
                newBalance,
                null
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error crediting wallet - Player: {PlayerId}", request.PlayerId);
            throw;
        }
    }

    public async Task<WalletOperationResponse> RollbackAsync(WalletRollbackRequest request)
    {
        _logger.LogInformation("Rolling back transaction - ExternalRefOriginal: {ExternalRef}", 
            request.ExternalRefOriginal);

        // 1. Buscar transacción original en WalletTransactions
        var originalTransaction = await _context.WalletTransactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == request.ExternalRefOriginal);

        if (originalTransaction == null)
        {
            throw new InvalidOperationException(
                $"Original transaction not found: {request.ExternalRefOriginal}"
            );
        }

        // 2. Verificar que no se haya revertido previamente
        var existingRollback = await _context.WalletTransactions
            .AnyAsync(t => 
                t.TransactionType == Domain.Enums.TransactionType.ROLLBACK && 
                t.Description != null && 
                t.Description.Contains(request.ExternalRefOriginal)
            );

        if (existingRollback)
        {
            _logger.LogWarning("Transaction already rolled back: {ExternalRef}", request.ExternalRefOriginal);
            
            Guid playerId = originalTransaction.FromUserId ?? originalTransaction.ToUserId;
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
            
            return new WalletOperationResponse(
                true,
                originalTransaction.Id,
                "Transaction already rolled back",
                player?.WalletBalance ?? 0,
                null
            );
        }

        // 3. Transacción DB
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // 4. Determinar operación inversa
            Guid playerId;
            bool wasDebit = originalTransaction.TransactionType == Domain.Enums.TransactionType.BET;
            decimal amount = originalTransaction.Amount;

            if (wasDebit)
            {
                playerId = originalTransaction.FromUserId!.Value;
            }
            else
            {
                playerId = originalTransaction.ToUserId;
            }

            // 5. Obtener y bloquear player
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                throw new InvalidOperationException($"Player {playerId} not found");
            }

            // 6. Capturar balance ANTES
            var previousBalance = player.WalletBalance;

            // 7. Invertir la operación
            if (wasDebit)
            {
                player.WalletBalance += amount; // Revertir débito = crédito
            }
            else
            {
                if (player.WalletBalance < amount)
                {
                    throw new InvalidOperationException(
                        $"Insufficient balance for rollback. Required: {amount}, Available: {player.WalletBalance}"
                    );
                }
                player.WalletBalance -= amount; // Revertir crédito = débito
            }

            var newBalance = player.WalletBalance;

            // 8. Crear registro ROLLBACK
            var rollbackTransaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                BrandId = player.BrandId,
                FromUserId = wasDebit ? Guid.Empty : player.Id,
                FromUserType = wasDebit ? "HOUSE" : "PLAYER",
                ToUserId = wasDebit ? player.Id : Guid.Empty,
                ToUserType = wasDebit ? "PLAYER" : "HOUSE",
                Amount = amount,
                TransactionType = Domain.Enums.TransactionType.ROLLBACK,
                PreviousBalanceFrom = wasDebit ? 0 : previousBalance,
                NewBalanceFrom = wasDebit ? 0 : newBalance,
                PreviousBalanceTo = wasDebit ? previousBalance : 0,
                NewBalanceTo = wasDebit ? newBalance : 0,
                Description = $"Rollback of transaction {request.ExternalRefOriginal}",
                CreatedByUserId = player.Id,
                CreatedByRole = "PLAYER",
                IdempotencyKey = $"rollback-{request.ExternalRefOriginal}-{Guid.NewGuid()}",
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(rollbackTransaction);

            // 9. Crear registro en Ledger
            long deltaBigint = (long)(amount * 100); // Convertir a centavos
            var ledgerEntry = new Ledger
            {
                BrandId = player.BrandId,
                PlayerId = player.Id,
                RoundId = null,
                DeltaBigint = wasDebit ? deltaBigint : -deltaBigint, // Invertir signo según operación
                Reason = LedgerReason.ROLLBACK,
                GameCode = null,
                Provider = "unified",
                ExternalRef = $"rollback-{request.ExternalRefOriginal}",
                Meta = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);

            // 10. Guardar y commit
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Rollback successful - Player: {PlayerId}, Amount: {Amount}, NewBalance: {Balance}", 
                player.Id, amount, newBalance);

            return new WalletOperationResponse(
                true,
                rollbackTransaction.Id,
                "Rollback successful",
                newBalance,
                null
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error rolling back transaction - ExternalRef: {ExternalRef}", 
                request.ExternalRefOriginal);
            throw;
        }
    }
}
