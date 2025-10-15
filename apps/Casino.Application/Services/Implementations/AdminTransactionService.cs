using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// Servicio de administración que usa UnifiedWalletService internamente
/// Proporciona API administrativa unificada con TransactionType completo
/// </summary>
public class AdminTransactionService : IAdminTransactionService
{
    private readonly IWalletService _unifiedWalletService;
    private readonly CasinoDbContext _context;
    private readonly ILogger<AdminTransactionService> _logger;

    public AdminTransactionService(
        IWalletService unifiedWalletService,
        CasinoDbContext context,
        ILogger<AdminTransactionService> logger)
    {
        _unifiedWalletService = unifiedWalletService;
        _context = context;
        _logger = logger;
    }

    public async Task<AdminTransactionResponse> CreateTransactionAsync(
        CreateAdminTransactionRequest request, 
        Guid actorUserId, 
        BackofficeUserRole actorRole, 
        Guid brandId)
    {
        _logger.LogInformation("Creating admin transaction - Type: {Type}, From: {From}, To: {To}, Amount: {Amount}", 
            request.TransactionType, request.FromUserId, request.ToUserId, request.Amount);

        // 1. Verificar idempotencia
        var existingTransaction = await _context.WalletTransactions
            .Include(t => t.Brand)
            .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey);

        if (existingTransaction != null)
        {
            _logger.LogInformation("Idempotent request detected: {IdempotencyKey}", request.IdempotencyKey);
            return await MapTransactionToAdminResponseAsync(existingTransaction, actorUserId, actorRole);
        }

        // 2. Validar autorización
        bool isMint = !request.FromUserId.HasValue;
        if (isMint && actorRole != BackofficeUserRole.SUPER_ADMIN)
        {
            throw new InvalidOperationException("Only SUPER_ADMIN can create MINT transactions");
        }

        // 3. Validar que usuarios existan y pertenezcan al brand correcto
        await ValidateUsersInBrandAsync(request.FromUserId, request.FromUserType, 
            request.ToUserId, request.ToUserType, brandId, actorRole);

        // 4. Comenzar transacción DB
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            // 5. Obtener y bloquear usuarios en orden
            var (fromUser, fromPlayer, toUser, toPlayer) = await LockUsersInOrderAsync(
                request.FromUserId, request.FromUserType, request.ToUserId, request.ToUserType, brandId);

            // 6. Capturar balances ANTES
            decimal? previousBalanceFrom = null;
            if (!isMint)
            {
                previousBalanceFrom = request.FromUserType == "BACKOFFICE" 
                    ? fromUser?.WalletBalance ?? 0 
                    : fromPlayer?.WalletBalance ?? 0;
            }
            
            decimal previousBalanceTo = request.ToUserType == "BACKOFFICE" 
                ? toUser?.WalletBalance ?? 0 
                : toPlayer?.WalletBalance ?? 0;

            // 7. Validar saldo suficiente
            if (!isMint)
            {
                if (previousBalanceFrom < request.Amount)
                {
                    throw new InvalidOperationException(
                        $"Insufficient balance. Required: {request.Amount}, Available: {previousBalanceFrom}");
                }
            }

            // 8. Actualizar balances
            if (!isMint)
            {
                if (request.FromUserType == "BACKOFFICE")
                    fromUser!.WalletBalance -= request.Amount;
                else
                    fromPlayer!.WalletBalance -= request.Amount;
            }

            if (request.ToUserType == "BACKOFFICE")
                toUser!.WalletBalance += request.Amount;
            else
                toPlayer!.WalletBalance += request.Amount;

            // 9. Capturar balances DESPUÉS
            decimal? newBalanceFrom = null;
            if (!isMint)
            {
                newBalanceFrom = request.FromUserType == "BACKOFFICE" 
                    ? fromUser?.WalletBalance ?? 0 
                    : fromPlayer?.WalletBalance ?? 0;
            }
            
            decimal newBalanceTo = request.ToUserType == "BACKOFFICE" 
                ? toUser?.WalletBalance ?? 0 
                : toPlayer?.WalletBalance ?? 0;

            // 10. Crear registro en WalletTransactions
            var walletTransaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                BrandId = brandId,
                FromUserId = request.FromUserId,
                FromUserType = request.FromUserType,
                ToUserId = request.ToUserId,
                ToUserType = request.ToUserType,
                Amount = request.Amount,
                TransactionType = request.TransactionType,
                PreviousBalanceFrom = previousBalanceFrom,
                NewBalanceFrom = newBalanceFrom,
                PreviousBalanceTo = previousBalanceTo,
                NewBalanceTo = newBalanceTo,
                Description = request.Description ?? DetermineTypeFromTransactionType(request.TransactionType),
                CreatedByUserId = actorUserId,
                CreatedByRole = actorRole.ToString(),
                IdempotencyKey = request.IdempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(walletTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Admin transaction created successfully: {TransactionId} - {Type} - Amount: {Amount}",
                walletTransaction.Id, request.TransactionType, request.Amount);

            return await MapTransactionToAdminResponseAsync(walletTransaction, actorUserId, actorRole);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating admin transaction");
            throw;
        }
    }

    public async Task<GetAdminTransactionsResponse> GetTransactionsAsync(
        GetAdminTransactionsRequest request, 
        Guid? brandScope, 
        Guid actorUserId, 
        BackofficeUserRole actorRole)
    {
        var query = _context.WalletTransactions
            .Include(t => t.Brand)
            .AsQueryable();

        // Aplicar filtro de brand scope
        if (brandScope.HasValue)
        {
            query = query.Where(t => t.BrandId == brandScope.Value);
        }

        // Aplicar filtros
        if (request.UserId.HasValue)
        {
            query = query.Where(t => 
                t.FromUserId == request.UserId || 
                t.ToUserId == request.UserId);
        }

        if (request.TransactionType.HasValue)
        {
            query = query.Where(t => t.TransactionType == request.TransactionType);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrEmpty(request.ExternalRef))
        {
            query = query.Where(t => t.IdempotencyKey.Contains(request.ExternalRef));
        }

        // Ordenar por fecha descendente
        query = query.OrderByDescending(t => t.CreatedAt);

        // Aplicar paginación
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var transactions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Obtener información de players y actors
        var playerIds = transactions
            .SelectMany(t => new[] { t.FromUserId, t.ToUserId })
            .Where(id => id != Guid.Empty && id != null)
            .Distinct()
            .ToList();

        var players = await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Username);

        var actorIds = transactions
            .Select(t => t.CreatedByUserId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var actors = await _context.BackofficeUsers
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        // Mapear a response
        var transactionResponses = transactions.Select(t =>
        {
            var playerId = t.ToUserType == "PLAYER" ? t.ToUserId : 
                          t.FromUserType == "PLAYER" ? t.FromUserId!.Value : Guid.Empty;
            
            var playerUsername = playerId != Guid.Empty && players.TryGetValue(playerId, out var username) 
                ? username : "Unknown";

            return new AdminTransactionResponse(
                t.Id,
                t.BrandId,
                DetermineTypeFromTransactionType(t.TransactionType ?? TransactionType.BET), // "MINT", "BET", "WIN", etc.
                t.FromUserId,
                t.FromUserType,
                t.FromUserId.HasValue && players.TryGetValue(t.FromUserId.Value, out var fromUsername) ? fromUsername : null,
                t.PreviousBalanceFrom,
                t.NewBalanceFrom,
                t.ToUserId,
                t.ToUserType,
                playerUsername,
                t.PreviousBalanceTo ?? 0,
                t.NewBalanceTo ?? 0,
                t.Amount,
                t.Description,
                t.TransactionType ?? TransactionType.BET,
                t.CreatedByUserId,
                actors.TryGetValue(t.CreatedByUserId, out var actorName) ? actorName : "Unknown",
                t.CreatedByRole ?? "Unknown",
                t.IdempotencyKey,
                t.CreatedAt
            );
        });

        return new GetAdminTransactionsResponse(
            transactionResponses,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages
        );
    }

    public async Task<AdminTransactionResponse> RollbackTransactionAsync(
        AdminRollbackRequest request, 
        Guid actorUserId, 
        BackofficeUserRole actorRole, 
        Guid brandId)
    {
        _logger.LogInformation("Rolling back transaction - ExternalRef: {ExternalRef}", request.ExternalRef);

        var rollbackRequest = new WalletRollbackRequest(request.ExternalRef);
        var walletResponse = await _unifiedWalletService.RollbackAsync(rollbackRequest);

        if (!walletResponse.Success)
        {
            throw new InvalidOperationException(walletResponse.ErrorMessage ?? "Rollback failed");
        }

        // Buscar la transacción de rollback creada
        var rollbackTransaction = await _context.WalletTransactions
            .Include(t => t.Brand)
            .Where(t => t.TransactionType == TransactionType.ROLLBACK)
            .Where(t => t.Description != null && t.Description.Contains(request.ExternalRef))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (rollbackTransaction == null)
        {
            throw new InvalidOperationException("Rollback was processed but transaction not found");
        }

        // Obtener información del player y actor
        var playerId = rollbackTransaction.ToUserType == "PLAYER" ? rollbackTransaction.ToUserId : 
                      rollbackTransaction.FromUserId!.Value;
        
        var player = await _context.Players
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == playerId);

        var actor = await _context.BackofficeUsers
            .FirstOrDefaultAsync(u => u.Id == actorUserId);

        return new AdminTransactionResponse(
            rollbackTransaction.Id,
            rollbackTransaction.BrandId,
            "ROLLBACK", // Tipo fijo para rollbacks
            rollbackTransaction.FromUserId,
            rollbackTransaction.FromUserType,
            rollbackTransaction.FromUserId.HasValue ? await GetUsernameAsync(rollbackTransaction.FromUserId.Value, rollbackTransaction.FromUserType!) : null,
            rollbackTransaction.PreviousBalanceFrom,
            rollbackTransaction.NewBalanceFrom,
            rollbackTransaction.ToUserId,
            rollbackTransaction.ToUserType,
            player?.Username ?? "Unknown",
            rollbackTransaction.PreviousBalanceTo ?? 0,
            rollbackTransaction.NewBalanceTo ?? 0,
            rollbackTransaction.Amount,
            rollbackTransaction.Description,
            TransactionType.ROLLBACK,
            actorUserId,
            actor?.Username ?? "Unknown",
            actorRole.ToString(),
            rollbackTransaction.IdempotencyKey,
            rollbackTransaction.CreatedAt
        );
    }

    public async Task<decimal> GetPlayerBalanceAsync(Guid playerId)
    {
        var balanceRequest = new WalletBalanceRequest(playerId);
        var response = await _unifiedWalletService.GetBalanceAsync(balanceRequest);
        return response.Balance;
    }

    public async Task<object?> GetUserBalanceAsync(Guid userId, string userType)
    {
        if (userType == "BACKOFFICE")
        {
            var backofficeUser = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (backofficeUser == null) return null;
            
            return new { 
                userId, 
                userType = "BACKOFFICE", 
                username = backofficeUser.Username, 
                balance = backofficeUser.WalletBalance 
            };
        }
        else if (userType == "PLAYER")
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == userId);
            
            if (player == null) return null;
            
            return new { 
                userId, 
                userType = "PLAYER", 
                username = player.Username, 
                balance = player.WalletBalance 
            };
        }

        return null;
    }

    /// <summary>
    /// Determina si un TransactionType es un débito (reduce el balance del jugador)
    /// </summary>
    private static bool IsDebitTransaction(TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.BET => true,
            TransactionType.WITHDRAWAL => true,
            TransactionType.ADJUSTMENT => false, // Podría ser ambos, por ahora crédito
            _ => false // WIN, DEPOSIT, MINT, TRANSFER, BONUS, ROLLBACK son créditos
        };
    }

    /// <summary>
    /// Convierte TransactionType a string compatible con SimpleWalletService
    /// Mantiene la estructura existente: "MINT", "TRANSFER", etc.
    /// </summary>
    private static string DetermineTypeFromTransactionType(TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.MINT => "MINT",
            TransactionType.TRANSFER => "TRANSFER", 
            TransactionType.BET => "BET",
            TransactionType.WIN => "WIN",
            TransactionType.DEPOSIT => "DEPOSIT",
            TransactionType.WITHDRAWAL => "WITHDRAWAL",
            TransactionType.BONUS => "BONUS",
            TransactionType.ADJUSTMENT => "ADJUSTMENT",
            TransactionType.ROLLBACK => "ROLLBACK",
            _ => "TRANSFER" // Default compatible
        };
    }

    /// <summary>
    /// Obtiene el username de un usuario según su tipo
    /// Reutiliza la lógica de SimpleWalletService
    /// </summary>
    private async Task<string> GetUsernameAsync(Guid userId, string userType)
    {
        if (userType == "BACKOFFICE")
        {
            var user = await _context.BackofficeUsers.FindAsync(userId);
            return user?.Username ?? "Unknown";
        }
        else if (userType == "PLAYER")
        {
            var player = await _context.Players.FindAsync(userId);
            return player?.Username ?? "Unknown";
        }

        return "Unknown";
    }

    /// <summary>
    /// Valida que los usuarios existan y pertenezcan al brand correcto
    /// </summary>
    private async Task ValidateUsersInBrandAsync(
        Guid? fromUserId, string? fromUserType,
        Guid toUserId, string toUserType,
        Guid brandId, BackofficeUserRole actorRole)
    {
        // Validar usuario origen (si no es MINT)
        if (fromUserId.HasValue)
        {
            if (fromUserType == "BACKOFFICE")
            {
                var user = await _context.BackofficeUsers
                    .FirstOrDefaultAsync(u => u.Id == fromUserId.Value);
                if (user == null)
                    throw new InvalidOperationException($"Source backoffice user {fromUserId} not found");
                
                if (actorRole != BackofficeUserRole.SUPER_ADMIN && user.BrandId != brandId && user.BrandId != null)
                    throw new InvalidOperationException("Cannot access user from different brand");
            }
            else if (fromUserType == "PLAYER")
            {
                var player = await _context.Players
                    .FirstOrDefaultAsync(p => p.Id == fromUserId.Value);
                if (player == null)
                    throw new InvalidOperationException($"Source player {fromUserId} not found");
                
                if (actorRole != BackofficeUserRole.SUPER_ADMIN && player.BrandId != brandId)
                    throw new InvalidOperationException("Cannot access player from different brand");
            }
        }

        // Validar usuario destino
        if (toUserType == "BACKOFFICE")
        {
            var user = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Id == toUserId);
            if (user == null)
                throw new InvalidOperationException($"Target backoffice user {toUserId} not found");
            
            if (actorRole != BackofficeUserRole.SUPER_ADMIN && user.BrandId != brandId && user.BrandId != null)
                throw new InvalidOperationException("Cannot access user from different brand");
        }
        else if (toUserType == "PLAYER")
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == toUserId);
            if (player == null)
                throw new InvalidOperationException($"Target player {toUserId} not found");
            
            if (actorRole != BackofficeUserRole.SUPER_ADMIN && player.BrandId != brandId)
                throw new InvalidOperationException("Cannot access player from different brand");
        }
    }

    /// <summary>
    /// Obtener y bloquear usuarios en orden consistente para evitar deadlocks
    /// </summary>
    private async Task<(BackofficeUser? fromUser, Player? fromPlayer, BackofficeUser? toUser, Player? toPlayer)> 
        LockUsersInOrderAsync(Guid? fromUserId, string? fromUserType, Guid toUserId, string toUserType, Guid brandId)
    {
        BackofficeUser? fromUser = null, toUser = null;
        Player? fromPlayer = null, toPlayer = null;

        // Obtener IDs en orden para bloquear consistentemente
        var userIds = new List<Guid>();
        if (fromUserId.HasValue) userIds.Add(fromUserId.Value);
        userIds.Add(toUserId);
        userIds = userIds.Distinct().OrderBy(id => id).ToList();

        // Bloquear registros en orden
        foreach (var userId in userIds)
        {
            if (fromUserId == userId && fromUserType == "BACKOFFICE")
            {
                fromUser = await _context.BackofficeUsers
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            else if (fromUserId == userId && fromUserType == "PLAYER")
            {
                fromPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.Id == userId);
            }

            if (toUserId == userId && toUserType == "BACKOFFICE")
            {
                toUser = await _context.BackofficeUsers
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            else if (toUserId == userId && toUserType == "PLAYER")
            {
                toPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.Id == userId);
            }
        }

        return (fromUser, fromPlayer, toUser, toPlayer);
    }

    /// <summary>
    /// Mapea WalletTransaction a AdminTransactionResponse
    /// </summary>
    private async Task<AdminTransactionResponse> MapTransactionToAdminResponseAsync(
        WalletTransaction transaction, Guid actorUserId, BackofficeUserRole actorRole)
    {
        var fromUsername = transaction.FromUserId.HasValue 
            ? await GetUsernameAsync(transaction.FromUserId.Value, transaction.FromUserType!) 
            : null;
        var toUsername = await GetUsernameAsync(transaction.ToUserId, transaction.ToUserType);
        
        var actor = await _context.BackofficeUsers.FindAsync(actorUserId);

        return new AdminTransactionResponse(
            transaction.Id,
            transaction.BrandId,
            DetermineTypeFromTransactionType(transaction.TransactionType ?? TransactionType.TRANSFER),
            transaction.FromUserId,
            transaction.FromUserType,
            fromUsername,
            transaction.PreviousBalanceFrom,
            transaction.NewBalanceFrom,
            transaction.ToUserId,
            transaction.ToUserType,
            toUsername,
            transaction.PreviousBalanceTo ?? 0,
            transaction.NewBalanceTo ?? 0,
            transaction.Amount,
            transaction.Description,
            transaction.TransactionType ?? TransactionType.TRANSFER,
            actorUserId,
            actor?.Username ?? "Unknown",
            actorRole.ToString(),
            transaction.IdempotencyKey,
            transaction.CreatedAt
        );
    }
}