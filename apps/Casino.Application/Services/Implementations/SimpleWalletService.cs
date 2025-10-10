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
/// SONNET: Servicio de transacciones SIMPLE+ con garantías críticas
/// Garantías: Idempotencia + Transacciones + Scope por brand + Locking
/// </summary>
public class SimpleWalletService : ISimpleWalletService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<SimpleWalletService> _logger;

    public SimpleWalletService(CasinoDbContext context, ILogger<SimpleWalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// SONNET: Método simplificado para endpoint con CreateTransactionRequest
    /// </summary>
    public async Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, 
        Guid actorUserId, BackofficeUserRole actorRole, Guid brandId)
    {
        return await TransferAsync(
            request.FromUserId, request.FromUserType,
            request.ToUserId, request.ToUserType,
            request.Amount, request.IdempotencyKey, request.Description,
            actorUserId, actorRole, brandId);
    }

    /// <summary>
    /// SONNET: Método principal de transferencia con todas las garantías
    /// SONNET: Registra balances before/after para auditoría completa
    /// </summary>
    public async Task<TransactionResponse> TransferAsync(
        Guid? fromUserId, string? fromUserType,
        Guid toUserId, string toUserType,
        decimal amount, string idempotencyKey, string? description,
        Guid actorUserId, BackofficeUserRole actorRole, Guid brandId,
        CancellationToken ct = default)
    {
        // SONNET: 1. Verificar idempotencia ANTES de comenzar transacción
        var existingTransaction = await _context.WalletTransactions
            .Include(t => t.Brand)
            .Include(t => t.CreatedByUser)
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);

        if (existingTransaction != null)
        {
            _logger.LogInformation("Idempotent request detected: {IdempotencyKey}", idempotencyKey);
            return await MapTransactionToResponseAsync(existingTransaction);
        }

        // SONNET: 2. Validar autorización y scope
        var authResult = await ValidateTransactionAuthorizationAsync(
            fromUserId, fromUserType, toUserId, toUserType, 
            actorUserId, actorRole, brandId);
        if (!authResult.IsAuthorized)
        {
            throw new InvalidOperationException(authResult.Message);
        }

        // SONNET: 3. Comenzar transacción con nivel SERIALIZABLE para máxima seguridad
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var operationType = !fromUserId.HasValue ? "MINT" : "TRANSFER";

            // SONNET: 4. Obtener y bloquear registros en orden consistente (evitar deadlocks)
            var (fromUser, fromPlayer, toUser, toPlayer) = await LockUsersInOrderAsync(
                fromUserId, fromUserType, toUserId, toUserType, brandId, ct);

            // SONNET: 5. Capturar balances ANTES de la transacción (para auditoría)
            decimal? previousBalanceFrom = null;
            decimal previousBalanceTo;
            
            if (operationType == "TRANSFER")
            {
                previousBalanceFrom = fromUserType == "BACKOFFICE" 
                    ? fromUser?.WalletBalance ?? 0 
                    : fromPlayer?.WalletBalance ?? 0;
            }
            
            previousBalanceTo = toUserType == "BACKOFFICE" 
                ? toUser?.WalletBalance ?? 0 
                : toPlayer?.WalletBalance ?? 0;

            // SONNET: 6. Validar saldo suficiente (si no es MINT)
            if (operationType == "TRANSFER")
            {
                await ValidateSufficientBalanceAsync(fromUserId!.Value, fromUserType!, amount, fromUser, fromPlayer);
            }

            // SONNET: 7. Actualizar balances
            await UpdateBalancesAsync(amount, fromUser, fromPlayer, toUser, toPlayer, operationType);

            // SONNET: 8. Capturar balances DESPUÉS de la transacción (para auditoría)
            decimal? newBalanceFrom = null;
            decimal newBalanceTo;
            
            if (operationType == "TRANSFER")
            {
                newBalanceFrom = fromUserType == "BACKOFFICE" 
                    ? fromUser?.WalletBalance ?? 0 
                    : fromPlayer?.WalletBalance ?? 0;
            }
            
            newBalanceTo = toUserType == "BACKOFFICE" 
                ? toUser?.WalletBalance ?? 0 
                : toPlayer?.WalletBalance ?? 0;

            // SONNET: 9. Crear registro de transacción con auditoría completa
            var walletTransaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                BrandId = brandId,
                FromUserId = fromUserId,
                FromUserType = fromUserType,
                ToUserId = toUserId,
                ToUserType = toUserType,
                Amount = amount,
                // SONNET: Campos de auditoría de balances
                PreviousBalanceFrom = previousBalanceFrom,
                NewBalanceFrom = newBalanceFrom,
                PreviousBalanceTo = previousBalanceTo,
                NewBalanceTo = newBalanceTo,
                Description = description ?? operationType,
                CreatedByUserId = actorUserId,
                CreatedByRole = actorRole.ToString(),
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(walletTransaction);
            await _context.SaveChangesAsync(ct);

            // SONNET: 10. Commit transacción
            await transaction.CommitAsync(ct);

            // Cargar relaciones para response
            await _context.Entry(walletTransaction)
                .Reference(t => t.Brand)
                .LoadAsync(ct);
            await _context.Entry(walletTransaction)
                .Reference(t => t.CreatedByUser)
                .LoadAsync(ct);

            _logger.LogInformation("Transaction created successfully: {TransactionId} - {OperationType} - Amount: {Amount} - From: {PrevFrom} ? {NewFrom} - To: {PrevTo} ? {NewTo}", 
                walletTransaction.Id, operationType, amount, previousBalanceFrom, newBalanceFrom, previousBalanceTo, newBalanceTo);

            return await MapTransactionToResponseAsync(walletTransaction);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error creating transaction - Actor: {ActorUserId} - IdempotencyKey: {IdempotencyKey}", 
                actorUserId, idempotencyKey);
            throw;
        }
    }

    public async Task<GetTransactionsResponse> GetTransactionsAsync(GetTransactionsRequest request, Guid? brandScope, Guid actorUserId, BackofficeUserRole actorRole)
    {
        var query = _context.WalletTransactions
            .Include(t => t.Brand)
            .Include(t => t.CreatedByUser)
            .AsQueryable();

        // SONNET: Aplicar scope por brand (SUPER_ADMIN puede usar GlobalScope)
        if (!request.GlobalScope || actorRole != BackofficeUserRole.SUPER_ADMIN)
        {
            if (brandScope.HasValue)
            {
                query = query.Where(t => t.BrandId == brandScope.Value);
            }
        }

        // SONNET: Para CASHIER, filtrar solo transacciones donde él participó
        if (actorRole == BackofficeUserRole.CASHIER)
        {
            query = query.Where(t => 
                t.CreatedByUserId == actorUserId ||
                t.FromUserId == actorUserId ||
                t.ToUserId == actorUserId);
        }

        // Aplicar filtros del request
        if (request.UserId.HasValue)
        {
            query = query.Where(t => t.FromUserId == request.UserId.Value || t.ToUserId == request.UserId.Value);
        }

        if (!string.IsNullOrEmpty(request.UserType))
        {
            query = query.Where(t => t.FromUserType == request.UserType || t.ToUserType == request.UserType);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            query = query.Where(t => t.Description != null && t.Description.Contains(request.Description));
        }

        var totalCount = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var responses = new List<TransactionResponse>();
        foreach (var trans in transactions)
        {
            responses.Add(await MapTransactionToResponseAsync(trans));
        }

        return new GetTransactionsResponse(
            responses,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize)
        );
    }

    public async Task<SimpleWalletBalanceResponse?> GetBalanceAsync(Guid userId, string userType)
    {
        if (userType == "BACKOFFICE")
        {
            var backofficeUser = await _context.BackofficeUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
            
            if (backofficeUser == null) return null;
            
            return new SimpleWalletBalanceResponse(
                userId, 
                "BACKOFFICE", 
                backofficeUser.Username, 
                backofficeUser.WalletBalance);
        }
        else if (userType == "PLAYER")
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == userId);
            
            if (player == null) return null;
            
            return new SimpleWalletBalanceResponse(
                userId, 
                "PLAYER", 
                player.Username, 
                player.WalletBalance);
        }

        return null;
    }

    // SONNET: Métodos auxiliares privados

    /// <summary>
    /// SONNET: Obtener y bloquear usuarios en orden consistente para evitar deadlocks
    /// </summary>
    private async Task<(BackofficeUser? fromUser, Player? fromPlayer, BackofficeUser? toUser, Player? toPlayer)> 
        LockUsersInOrderAsync(Guid? fromUserId, string? fromUserType, Guid toUserId, string toUserType, 
        Guid brandId, CancellationToken ct)
    {
        BackofficeUser? fromUser = null, toUser = null;
        Player? fromPlayer = null, toPlayer = null;

        // SONNET: Obtener IDs en orden para bloquear consistentemente
        var userIds = new List<Guid>();
        if (fromUserId.HasValue) userIds.Add(fromUserId.Value);
        userIds.Add(toUserId);
        userIds = userIds.Distinct().OrderBy(id => id).ToList();

        // SONNET: Bloquear registros en orden (FOR UPDATE equivalente en EF)
        foreach (var userId in userIds)
        {
            if (fromUserId == userId && fromUserType == "BACKOFFICE")
            {
                fromUser = await _context.BackofficeUsers
                    .Where(u => u.Id == userId && (u.BrandId == brandId || u.BrandId == null)) // SUPER_ADMIN no tiene brand
                    .FirstOrDefaultAsync(ct);
                if (fromUser == null)
                    throw new InvalidOperationException($"Source backoffice user {userId} not found in brand {brandId}");
            }
            else if (fromUserId == userId && fromUserType == "PLAYER")
            {
                fromPlayer = await _context.Players
                    .Where(p => p.Id == userId && p.BrandId == brandId)
                    .FirstOrDefaultAsync(ct);
                if (fromPlayer == null)
                    throw new InvalidOperationException($"Source player {userId} not found in brand {brandId}");
            }

            if (toUserId == userId && toUserType == "BACKOFFICE")
            {
                toUser = await _context.BackofficeUsers
                    .Where(u => u.Id == userId && (u.BrandId == brandId || u.BrandId == null))
                    .FirstOrDefaultAsync(ct);
                if (toUser == null)
                    throw new InvalidOperationException($"Target backoffice user {userId} not found in brand {brandId}");
            }
            else if (toUserId == userId && toUserType == "PLAYER")
            {
                toPlayer = await _context.Players
                    .Where(p => p.Id == userId && p.BrandId == brandId)
                    .FirstOrDefaultAsync(ct);
                if (toPlayer == null)
                    throw new InvalidOperationException($"Target player {userId} not found in brand {brandId}");
            }
        }

        return (fromUser, fromPlayer, toUser, toPlayer);
    }

    /// <summary>
    /// SONNET: Validar saldo suficiente antes de transferir
    /// </summary>
    private async Task ValidateSufficientBalanceAsync(Guid fromUserId, string fromUserType, decimal amount, 
        BackofficeUser? fromUser, Player? fromPlayer)
    {
        decimal currentBalance = fromUserType == "BACKOFFICE" 
            ? fromUser?.WalletBalance ?? 0 
            : fromPlayer?.WalletBalance ?? 0;

        if (currentBalance < amount)
        {
            throw new InvalidOperationException($"Insufficient balance. Required: {amount}, Available: {currentBalance}");
        }
    }

    /// <summary>
    /// SONNET: Actualizar balances en las entidades correspondientes
    /// </summary>
    private async Task UpdateBalancesAsync(decimal amount, 
        BackofficeUser? fromUser, Player? fromPlayer, BackofficeUser? toUser, Player? toPlayer, string operationType)
    {
        // Débito (si no es MINT)
        if (operationType == "TRANSFER")
        {
            if (fromUser != null)
            {
                fromUser.WalletBalance -= amount;
            }
            else if (fromPlayer != null)
            {
                fromPlayer.WalletBalance -= amount;
            }
        }

        // Crédito
        if (toUser != null)
        {
            toUser.WalletBalance += amount;
        }
        else if (toPlayer != null)
        {
            toPlayer.WalletBalance += amount;
        }
    }

    /// <summary>
    /// SONNET: Validar autorización según roles y scope de brand
    /// </summary>
    private async Task<(bool IsAuthorized, string Message)> ValidateTransactionAuthorizationAsync(
        Guid? fromUserId, string? fromUserType, Guid toUserId, string toUserType,
        Guid actorUserId, BackofficeUserRole actorRole, Guid brandId)
    {
        bool isMint = !fromUserId.HasValue;

        // SONNET: Solo SUPER_ADMIN puede hacer MINT
        if (isMint && actorRole != BackofficeUserRole.SUPER_ADMIN)
        {
            return (false, "Only SUPER_ADMIN can create money (MINT)");
        }

        // SONNET: SUPER_ADMIN puede hacer cualquier transacción
        if (actorRole == BackofficeUserRole.SUPER_ADMIN)
        {
            return (true, "Authorized");
        }

        // SONNET: BRAND_ADMIN puede transferir entre usuarios de su brand
        if (actorRole == BackofficeUserRole.BRAND_ADMIN)
        {
            // Validar que ambos usuarios pertenezcan al brand
            if (!isMint)
            {
                var fromInBrand = await IsUserInBrandAsync(fromUserId!.Value, fromUserType!, brandId);
                if (!fromInBrand)
                {
                    return (false, "Source user not in actor's brand");
                }
            }

            var toInBrand = await IsUserInBrandAsync(toUserId, toUserType, brandId);
            if (!toInBrand)
            {
                return (false, "Target user not in actor's brand");
            }

            return (true, "Authorized");
        }

        // SONNET: CASHIER solo puede transferir con players de su brand
        if (actorRole == BackofficeUserRole.CASHIER)
        {
            if (toUserType != "PLAYER" || (!isMint && fromUserType != "BACKOFFICE"))
            {
                return (false, "CASHIER can only transfer with PLAYER users");
            }

            var toInBrand = await IsUserInBrandAsync(toUserId, toUserType, brandId);
            if (!toInBrand)
            {
                return (false, "Player not in cashier's brand");
            }

            return (true, "Authorized");
        }

        return (false, "Insufficient privileges");
    }

    private async Task<bool> IsUserInBrandAsync(Guid userId, string userType, Guid brandId)
    {
        if (userType == "BACKOFFICE")
        {
            return await _context.BackofficeUsers
                .AnyAsync(u => u.Id == userId && (u.BrandId == brandId || u.BrandId == null)); // SUPER_ADMIN no tiene brand
        }
        else if (userType == "PLAYER")
        {
            return await _context.Players
                .AnyAsync(p => p.Id == userId && p.BrandId == brandId);
        }

        return false;
    }

    private async Task<TransactionResponse> MapTransactionToResponseAsync(WalletTransaction transaction)
    {
        var fromUsername = transaction.FromUserId.HasValue ? await GetUsernameAsync(transaction.FromUserId.Value, transaction.FromUserType!) : null;
        var toUsername = await GetUsernameAsync(transaction.ToUserId, transaction.ToUserType);

        return new TransactionResponse(
            transaction.Id,
            transaction.BrandId,
            !transaction.FromUserId.HasValue ? "MINT" : "TRANSFER",
            transaction.FromUserId,
            transaction.FromUserType,
            fromUsername,
            // SONNET: Incluir balances before/after para auditoría
            transaction.PreviousBalanceFrom,
            transaction.NewBalanceFrom,
            transaction.ToUserId,
            transaction.ToUserType,
            toUsername,
            transaction.PreviousBalanceTo,
            transaction.NewBalanceTo,
            transaction.Amount,
            transaction.Description,
            transaction.CreatedByUserId,
            transaction.CreatedByUser.Username,
            transaction.CreatedByRole,
            transaction.IdempotencyKey,
            transaction.CreatedAt
        );
    }

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
}