using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// SONNET: Servicio de wallet legado que usa el sistema bigint existente
/// MANTIENE COMPATIBILIDAD EXCLUSIVA con Gateway endpoints
/// Usa Wallet.BalanceBigint (centavos) para providers externos
/// </summary>
public class LegacyWalletService : ILegacyWalletService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<LegacyWalletService> _logger;

    public LegacyWalletService(CasinoDbContext context, ILogger<LegacyWalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WalletBalanceResponse> GetBalanceAsync(WalletBalanceRequest request)
    {
        // SONNET: Usar Wallet.BalanceBigint (sistema legado para gateway)
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);

        if (wallet == null)
        {
            // Si no existe wallet, crear uno con balance 0
            wallet = new Wallet
            {
                PlayerId = request.PlayerId,
                BalanceBigint = 0
            };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        // SONNET: Convertir de centavos (bigint) a decimal para respuesta
        var balanceDecimal = wallet.BalanceBigint / 100.0m;
        return new WalletBalanceResponse(balanceDecimal);
    }

    public async Task<WalletOperationResponse> DebitAsync(WalletDebitRequest request)
    {
        // SONNET: Usar transacción DB con nivel SERIALIZABLE para máxima seguridad
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            // SONNET: Verificar si ya existe una transacción con el mismo ExternalRef (idempotencia)
            if (!string.IsNullOrEmpty(request.ExternalRef))
            {
                var existingLedger = await _context.Ledger
                    .FirstOrDefaultAsync(l => l.ExternalRef == request.ExternalRef);

                if (existingLedger != null)
                {
                    // Transacción ya procesada (idempotencia)
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);
                    var currentBalance = wallet?.BalanceBigint / 100.0m ?? 0;

                    return new WalletOperationResponse(
                        true, 
                        new Guid(existingLedger.Id.ToString()), 
                        "Transaction already processed", 
                        currentBalance, 
                        null);
                }
            }

            // SONNET: Obtener wallet con bloqueo
            var playerWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);

            if (playerWallet == null)
            {
                return new WalletOperationResponse(
                    false, 
                    null, 
                    "Wallet not found", 
                    null, 
                    "Player wallet not found");
            }

            // SONNET: Verificar saldo suficiente
            if (playerWallet.BalanceBigint < request.Amount)
            {
                var currentBalance = playerWallet.BalanceBigint / 100.0m;
                return new WalletOperationResponse(
                    false, 
                    null, 
                    "Insufficient balance", 
                    currentBalance, 
                    "Insufficient balance");
            }

            // SONNET: Actualizar balance bigint
            playerWallet.BalanceBigint -= request.Amount;

            // SONNET: Crear entrada en ledger
            var ledgerEntry = new Ledger
            {
                PlayerId = request.PlayerId,
                BrandId = (await _context.Players.FindAsync(request.PlayerId))?.BrandId ?? Guid.Empty,
                DeltaBigint = -request.Amount, // Negativo para débito
                Reason = Enum.Parse<LedgerReason>(request.Reason),
                ExternalRef = request.ExternalRef,
                GameCode = request.GameRoundId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);
            await _context.SaveChangesAsync();

            // SONNET: Commit transacción
            await transaction.CommitAsync();

            var finalBalance = playerWallet.BalanceBigint / 100.0m;
            _logger.LogInformation("Legacy debit successful: PlayerId={PlayerId}, Amount={Amount}, NewBalance={Balance}", 
                request.PlayerId, request.Amount, finalBalance);

            return new WalletOperationResponse(
                true, 
                new Guid(ledgerEntry.Id.ToString()), 
                "Debit successful", 
                finalBalance, 
                null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing legacy debit for PlayerId={PlayerId}", request.PlayerId);
            return new WalletOperationResponse(
                false, 
                null, 
                "Internal error", 
                null, 
                "An error occurred processing the transaction");
        }
    }

    public async Task<WalletOperationResponse> CreditAsync(WalletCreditRequest request)
    {
        // SONNET: Usar transacción DB con nivel SERIALIZABLE
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            // SONNET: Verificar idempotencia
            if (!string.IsNullOrEmpty(request.ExternalRef))
            {
                var existingLedger = await _context.Ledger
                    .FirstOrDefaultAsync(l => l.ExternalRef == request.ExternalRef);

                if (existingLedger != null)
                {
                    // Transacción ya procesada
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);
                    var currentBalance = wallet?.BalanceBigint / 100.0m ?? 0;

                    return new WalletOperationResponse(
                        true, 
                        new Guid(existingLedger.Id.ToString()), 
                        "Transaction already processed", 
                        currentBalance, 
                        null);
                }
            }

            // SONNET: Obtener o crear wallet
            var playerWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.PlayerId == request.PlayerId);

            if (playerWallet == null)
            {
                playerWallet = new Wallet
                {
                    PlayerId = request.PlayerId,
                    BalanceBigint = 0
                };
                _context.Wallets.Add(playerWallet);
            }

            // SONNET: Actualizar balance bigint
            playerWallet.BalanceBigint += request.Amount;

            // SONNET: Crear entrada en ledger
            var ledgerEntry = new Ledger
            {
                PlayerId = request.PlayerId,
                BrandId = (await _context.Players.FindAsync(request.PlayerId))?.BrandId ?? Guid.Empty,
                DeltaBigint = request.Amount, // Positivo para crédito
                Reason = Enum.Parse<LedgerReason>(request.Reason),
                ExternalRef = request.ExternalRef,
                GameCode = request.GameRoundId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);
            await _context.SaveChangesAsync();

            // SONNET: Commit transacción
            await transaction.CommitAsync();

            var finalBalance = playerWallet.BalanceBigint / 100.0m;
            _logger.LogInformation("Legacy credit successful: PlayerId={PlayerId}, Amount={Amount}, NewBalance={Balance}", 
                request.PlayerId, request.Amount, finalBalance);

            return new WalletOperationResponse(
                true, 
                new Guid(ledgerEntry.Id.ToString()), 
                "Credit successful", 
                finalBalance, 
                null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing legacy credit for PlayerId={PlayerId}", request.PlayerId);
            return new WalletOperationResponse(
                false, 
                null, 
                "Internal error", 
                null, 
                "An error occurred processing the transaction");
        }
    }

    public async Task<WalletOperationResponse> RollbackAsync(WalletRollbackRequest request)
    {
        // SONNET: Usar transacción DB con nivel SERIALIZABLE
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            // SONNET: Buscar la transacción original
            var originalLedger = await _context.Ledger
                .FirstOrDefaultAsync(l => l.ExternalRef == request.ExternalRefOriginal);

            if (originalLedger == null)
            {
                return new WalletOperationResponse(
                    false, 
                    null, 
                    "Original transaction not found", 
                    null, 
                    "Original transaction not found");
            }

            // SONNET: Verificar si ya existe un rollback (idempotencia)
            var existingRollback = await _context.Ledger
                .FirstOrDefaultAsync(l => l.ExternalRef == $"ROLLBACK_{request.ExternalRefOriginal}");

            if (existingRollback != null)
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.PlayerId == originalLedger.PlayerId);
                var currentBalance = wallet?.BalanceBigint / 100.0m ?? 0;

                return new WalletOperationResponse(
                    true, 
                    new Guid(existingRollback.Id.ToString()), 
                    "Rollback already processed", 
                    currentBalance, 
                    null);
            }

            // SONNET: Obtener wallet
            var playerWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.PlayerId == originalLedger.PlayerId);

            if (playerWallet == null)
            {
                return new WalletOperationResponse(
                    false, 
                    null, 
                    "Wallet not found", 
                    null, 
                    "Player wallet not found");
            }

            // SONNET: Revertir la transacción (invertir el signo del delta original)
            playerWallet.BalanceBigint -= originalLedger.DeltaBigint;

            // SONNET: Crear entrada de rollback en ledger
            var rollbackEntry = new Ledger
            {
                PlayerId = originalLedger.PlayerId,
                BrandId = originalLedger.BrandId,
                DeltaBigint = -originalLedger.DeltaBigint, // Inverso del original
                Reason = LedgerReason.ROLLBACK,
                ExternalRef = $"ROLLBACK_{request.ExternalRefOriginal}",
                GameCode = originalLedger.GameCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(rollbackEntry);
            await _context.SaveChangesAsync();

            // SONNET: Commit transacción
            await transaction.CommitAsync();

            var finalBalance = playerWallet.BalanceBigint / 100.0m;
            _logger.LogInformation("Legacy rollback successful: ExternalRef={ExternalRef}, PlayerId={PlayerId}, NewBalance={Balance}", 
                request.ExternalRefOriginal, originalLedger.PlayerId, finalBalance);

            return new WalletOperationResponse(
                true, 
                new Guid(rollbackEntry.Id.ToString()), 
                "Rollback successful", 
                finalBalance, 
                null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing legacy rollback for ExternalRef={ExternalRef}", request.ExternalRefOriginal);
            return new WalletOperationResponse(
                false, 
                null, 
                "Internal error", 
                null, 
                "An error occurred processing the rollback");
        }
    }
}