using Casino.Application.DTOs.Player;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Casino.Application.Services.Implementations;

public class PlayerService : IPlayerService
{
    private readonly CasinoDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(
        CasinoDbContext context,
        IAuditService auditService,
        ILogger<PlayerService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetPlayerResponse> CreatePlayerAsync(CreatePlayerRequest request, Guid currentUserId, Guid effectiveBrandId, BackofficeUserRole? currentUserRole = null)
    {
        // Verificar que el username no esté en uso en este brand
        var existingPlayer = await _context.Players
            .FirstOrDefaultAsync(p => p.Username == request.Username && p.BrandId == effectiveBrandId);

        if (existingPlayer != null)
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists in this brand");
        }

        // Verificar externalId si se proporciona
        if (!string.IsNullOrEmpty(request.ExternalId))
        {
            var existingExternalId = await _context.Players
                .FirstOrDefaultAsync(p => p.ExternalId == request.ExternalId && p.BrandId == effectiveBrandId);

            if (existingExternalId != null)
            {
                throw new InvalidOperationException($"External ID '{request.ExternalId}' already exists in this brand");
            }
        }

        // Verificar que el brand existe y está activo
        var brand = await _context.Brands
            .FirstOrDefaultAsync(b => b.Id == effectiveBrandId && b.Status == BrandStatus.ACTIVE);

        if (brand == null)
        {
            throw new InvalidOperationException("Brand not found or inactive");
        }

        var playerId = Guid.NewGuid();

        // SONNET: Guardar siempre CreatedByUserId y CreatedByRole para auditoría
        var createdByUserId = currentUserId;
        var createdByRole = currentUserRole?.ToString();

        // Crear player
        var player = new Player
        {
            Id = playerId,
            BrandId = effectiveBrandId,
            Username = request.Username,
            Email = request.Email,
            ExternalId = request.ExternalId,
            Status = request.Status,
            // SONNET: Guardar auditoría de creación
            CreatedByUserId = createdByUserId,
            CreatedByRole = createdByRole,
            CreatedAt = DateTime.UtcNow
        };

        _context.Players.Add(player);

        // SONNET: Crear wallet legacy (bigint) para compatibilidad con gateway
        var wallet = new Wallet
        {
            PlayerId = playerId,
            BalanceBigint = request.InitialBalance
        };

        _context.Wallets.Add(wallet);

        // Si hay balance inicial, crear entrada en ledger
        if (request.InitialBalance != 0)
        {
            var ledgerEntry = new Ledger
            {
                BrandId = effectiveBrandId,
                PlayerId = playerId,
                DeltaBigint = request.InitialBalance,
                Reason = LedgerReason.ADMIN_GRANT,
                ExternalRef = $"initial_balance_{playerId}",
                GameCode = null,
                Provider = null,
                RoundId = null,
                Meta = JsonDocument.Parse($"{{\"description\":\"Initial balance\",\"created_by\":\"{currentUserId}\",\"created_by_role\":\"{createdByRole}\"}}"),
                CreatedAt = DateTime.UtcNow
            };

            _context.Ledger.Add(ledgerEntry);
        }

        await _context.SaveChangesAsync();

        // Cargar brand para respuesta
        await _context.Entry(player)
            .Reference(p => p.Brand)
            .LoadAsync();

        // Auditar
        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "CREATE_PLAYER",
            "Player",
            player.Id.ToString(),
            new { 
                request.Username, 
                request.Email,
                request.ExternalId,
                BrandId = effectiveBrandId,
                BrandName = brand.Name,
                InitialBalance = request.InitialBalance,
                CreatedByUserId = createdByUserId,
                CreatedByRole = createdByRole
            });

        _logger.LogInformation("Player created: {PlayerId} - {Username} in brand {BrandId} by user {CreatedByUserId} (role: {Role})",
            player.Id, player.Username, effectiveBrandId, currentUserId, createdByRole);

        return new GetPlayerResponse(
            player.Id,
            player.BrandId,
            brand.Code,
            brand.Name,
            player.Username,
            player.Email,
            player.ExternalId,
            player.Status,
            request.InitialBalance,
            player.CreatedAt);
    }

    public async Task<bool> DeletePlayerAsync(Guid playerId, Guid currentUserId, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .AsQueryable();

        // Aplicar scope por brand si es necesario
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            return false;

        // Verificar que no tenga actividad reciente (opcional - política de negocio)
        var hasRecentActivity = await _context.GameSessions
            .AnyAsync(gs => gs.PlayerId == playerId && gs.CreatedAt > DateTime.UtcNow.AddDays(-30));

        if (hasRecentActivity)
        {
            throw new InvalidOperationException("Cannot delete player with recent gaming activity");
        }

        // Eliminar entradas relacionadas en orden correcto
        // 1. Eliminar sesiones de juego
        var sessions = await _context.GameSessions
            .Where(gs => gs.PlayerId == playerId)
            .ToListAsync();
        _context.GameSessions.RemoveRange(sessions);

        // 2. Eliminar entradas de ledger
        var ledgerEntries = await _context.Ledger
            .Where(l => l.PlayerId == playerId)
            .ToListAsync();
        _context.Ledger.RemoveRange(ledgerEntries);

        // 3. Eliminar wallet
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.PlayerId == playerId);
        if (wallet != null)
        {
            _context.Wallets.Remove(wallet);
        }

        // 4. Eliminar player
        _context.Players.Remove(player);

        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "DELETE_PLAYER",
            "Player",
            player.Id.ToString(),
            new { 
                player.Username, 
                player.Email,
                BrandId = player.BrandId,
                BrandName = player.Brand?.Name,
                DeletedAt = DateTime.UtcNow
            });

        _logger.LogInformation("Player deleted: {PlayerId} - {Username} by user {DeletedByUserId}",
            player.Id, player.Username, currentUserId);

        return true;
    }

    public async Task<QueryPlayersResponse> GetPlayersAsync(QueryPlayersRequest request, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        // Aplicar filtros adicionales
        if (!string.IsNullOrEmpty(request.Username))
        {
            query = query.Where(p => p.Username.Contains(request.Username));
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            query = query.Where(p => p.Email!.Contains(request.Email));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var players = await query
            .OrderBy(p => p.Username)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var playerResponses = players.Select(p => new GetPlayerResponse(
            p.Id,
            p.BrandId,
            p.Brand.Code,
            p.Brand.Name,
            p.Username,
            p.Email,
            p.ExternalId, // Usar el ExternalId real del player
            p.Status,
            p.Wallet?.BalanceBigint ?? 0,
            p.CreatedAt
        ));

        return new QueryPlayersResponse(
            playerResponses,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }

    public async Task<GetPlayerResponse?> GetPlayerAsync(Guid playerId, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
            return null;

        return new GetPlayerResponse(
            player.Id,
            player.BrandId,
            player.Brand.Code,
            player.Brand.Name,
            player.Username,
            player.Email,
            player.ExternalId, // Usar el ExternalId real del player
            player.Status,
            player.Wallet?.BalanceBigint ?? 0,
            player.CreatedAt);
    }

    public async Task<GetPlayerResponse> UpdatePlayerAsync(Guid playerId, UpdatePlayerRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
        {
            throw new InvalidOperationException("Player not found or access denied");
        }

        var changes = new Dictionary<string, object>();

        // Cambio de username
        if (!string.IsNullOrEmpty(request.Username) && request.Username != player.Username)
        {
            var existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.BrandId == player.BrandId && p.Username == request.Username && p.Id != playerId);

            if (existingPlayer != null)
            {
                throw new InvalidOperationException($"Username '{request.Username}' already exists in brand '{player.Brand.Code}'");
            }

            changes["Username"] = new { Old = player.Username, New = request.Username };
            player.Username = request.Username;
        }

        // Cambio de email
        if (request.Email != player.Email)
        {
            changes["Email"] = new { Old = player.Email, New = request.Email };
            player.Email = request.Email;
        }

        // Cambio de status
        if (request.Status.HasValue && request.Status.Value != player.Status)
        {
            changes["Status"] = new { Old = player.Status, New = request.Status.Value };
            player.Status = request.Status.Value;
        }

        if (changes.Any())
        {
            await _context.SaveChangesAsync();

            await _auditService.LogBackofficeActionAsync(
                currentUserId,
                "UPDATE_PLAYER",
                "Player",
                player.Id.ToString(),
                changes);

            _logger.LogInformation("Player updated: {PlayerId} by user {UserId}", player.Id, currentUserId);
        }

        return new GetPlayerResponse(
            player.Id,
            player.BrandId,
            player.Brand.Code,
            player.Brand.Name,
            player.Username,
            player.Email,
            player.ExternalId, // Usar el ExternalId real del player
            player.Status,
            player.Wallet?.BalanceBigint ?? 0,
            player.CreatedAt);
    }

    public async Task<WalletAdjustmentResponse> AdjustPlayerWalletAsync(Guid playerId, AdjustPlayerWalletRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var query = _context.Players
            .Include(p => p.Brand)
            .Include(p => p.Wallet)
            .AsQueryable();

        // Aplicar scope por brand
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
        {
            return new WalletAdjustmentResponse(false, 0, 0, "Player not found or access denied");
        }

        // Verificar que el wallet existe
        if (player.Wallet == null)
        {
            player.Wallet = new Wallet { PlayerId = player.Id, BalanceBigint = 0 };
            _context.Wallets.Add(player.Wallet);
        }

        // Verificar saldo suficiente para débitos
        if (request.Amount < 0 && player.Wallet.BalanceBigint + request.Amount < 0)
        {
            return new WalletAdjustmentResponse(false, player.Wallet.BalanceBigint, 0, "Insufficient balance for this adjustment");
        }

        // Crear entrada en el ledger
        var ledgerEntry = new Ledger
        {
            BrandId = player.BrandId,
            PlayerId = player.Id,
            DeltaBigint = request.Amount,
            Reason = LedgerReason.ADJUST,
            ExternalRef = $"admin_adjustment_{Guid.NewGuid()}",
            Meta = JsonSerializer.SerializeToDocument(new { 
                Reason = request.Reason,
                AdjustedBy = currentUserId,
                Timestamp = DateTime.UtcNow
            }),
            CreatedAt = DateTime.UtcNow
        };

        _context.Ledger.Add(ledgerEntry);

        // Actualizar saldo del wallet
        player.Wallet.BalanceBigint += request.Amount;

        await _context.SaveChangesAsync();

        // Auditoría
        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "ADJUST_PLAYER_WALLET",
            "Player",
            player.Id.ToString(),
            new { 
                Amount = request.Amount,
                Reason = request.Reason,
                PreviousBalance = player.Wallet.BalanceBigint - request.Amount,
                NewBalance = player.Wallet.BalanceBigint,
                LedgerEntryId = ledgerEntry.Id
            });

        _logger.LogInformation("Player wallet adjusted: {PlayerId} - Amount: {Amount} - Reason: {Reason} by user {UserId}",
            player.Id, request.Amount, request.Reason, currentUserId);

        return new WalletAdjustmentResponse(
            true, 
            player.Wallet.BalanceBigint,
            ledgerEntry.Id,
            "Wallet adjusted successfully");
    }
}