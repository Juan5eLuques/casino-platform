using Casino.Application.DTOs.Cashier;
using Casino.Application.DTOs.Player;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class CashierPlayerService : ICashierPlayerService
{
    private readonly CasinoDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<CashierPlayerService> _logger;

    public CashierPlayerService(
        CasinoDbContext context,
        IAuditService auditService,
        ILogger<CashierPlayerService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AssignPlayerToCashierResponse> AssignPlayerAsync(Guid cashierId, Guid playerId, Guid currentUserId)
    {
        // Verificar que el cajero existe y es CASHIER
        var cashier = await _context.BackofficeUsers
            .FirstOrDefaultAsync(u => u.Id == cashierId && u.Role == BackofficeUserRole.CASHIER && u.Status == BackofficeUserStatus.ACTIVE);

        if (cashier == null)
        {
            throw new InvalidOperationException("Cashier not found or not active");
        }

        // Verificar que el jugador existe
        var player = await _context.Players
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == playerId && p.Status == PlayerStatus.ACTIVE);

        if (player == null)
        {
            throw new InvalidOperationException("Player not found or not active");
        }

        // Verificar que no existe ya la asignación
        var existingAssignment = await _context.CashierPlayers
            .FirstOrDefaultAsync(cp => cp.CashierId == cashierId && cp.PlayerId == playerId);

        if (existingAssignment != null)
        {
            throw new InvalidOperationException($"Player '{player.Username}' is already assigned to cashier '{cashier.Username}'");
        }

        // Crear la asignación
        var assignment = new CashierPlayer
        {
            CashierId = cashierId,
            PlayerId = playerId,
            AssignedAt = DateTime.UtcNow
        };

        _context.CashierPlayers.Add(assignment);
        await _context.SaveChangesAsync();

        // Auditoría
        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "ASSIGN_PLAYER_TO_CASHIER",
            "CashierPlayer",
            $"{cashierId}-{playerId}",
            new { CashierId = cashierId, PlayerId = playerId, CashierUsername = cashier.Username, PlayerUsername = player.Username });

        _logger.LogInformation("Player {PlayerUsername} assigned to cashier {CashierUsername} by user {UserId}",
            player.Username, cashier.Username, currentUserId);

        return new AssignPlayerToCashierResponse(
            cashierId,
            playerId,
            cashier.Username,
            player.Username,
            assignment.AssignedAt
        );
    }

    public async Task<GetCashierPlayersResponse> GetCashierPlayersAsync(Guid cashierId, Guid? brandScope)
    {
        var query = _context.BackofficeUsers
            .Include(u => u.CashierPlayers)
                .ThenInclude(cp => cp.Player)
                    .ThenInclude(p => p.Wallet)
            .Where(u => u.Id == cashierId && u.Role == BackofficeUserRole.CASHIER);

        // Aplicar scope de brand si es necesario
        if (brandScope.HasValue)
        {
            query = query.Where(u => u.BrandId == brandScope.Value);
        }

        var cashier = await query.FirstOrDefaultAsync();

        if (cashier == null)
        {
            throw new InvalidOperationException("Cashier not found or access denied");
        }

        var players = cashier.CashierPlayers.Select(cp => new CashierPlayerDto(
            cp.PlayerId,
            cp.Player.Username,
            cp.Player.Email ?? "",
            cp.Player.Status,
            cp.Player.Wallet?.BalanceBigint ?? 0,
            cp.AssignedAt
        ));

        return new GetCashierPlayersResponse(
            cashierId,
            cashier.Username,
            cashier.Role.ToString(),
            players
        );
    }

    public async Task<GetPlayerCashiersResponse> GetPlayerCashiersAsync(Guid playerId, Guid? brandScope)
    {
        var query = _context.Players
            .Include(p => p.CashierPlayers)
                .ThenInclude(cp => cp.Cashier)
            .Where(p => p.Id == playerId);

        // Aplicar scope de brand si es necesario
        if (brandScope.HasValue)
        {
            query = query.Where(p => p.BrandId == brandScope.Value);
        }

        var player = await query.FirstOrDefaultAsync();

        if (player == null)
        {
            throw new InvalidOperationException("Player not found or access denied");
        }

        var cashiers = player.CashierPlayers.Select(cp => new PlayerCashierDto(
            cp.CashierId,
            cp.Cashier.Username,
            cp.Cashier.Role,
            cp.AssignedAt
        ));

        return new GetPlayerCashiersResponse(
            playerId,
            player.Username,
            cashiers
        );
    }

    public async Task<UnassignPlayerResponse> UnassignPlayerAsync(Guid cashierId, Guid playerId, Guid currentUserId, Guid? brandScope)
    {
        var query = _context.CashierPlayers
            .Include(cp => cp.Cashier)
            .Include(cp => cp.Player)
            .Where(cp => cp.CashierId == cashierId && cp.PlayerId == playerId);

        // Aplicar scope de brand si es necesario
        if (brandScope.HasValue)
        {
            query = query.Where(cp => cp.Cashier.BrandId == brandScope.Value);
        }

        var assignment = await query.FirstOrDefaultAsync();

        if (assignment == null)
        {
            return new UnassignPlayerResponse(false, "Assignment not found or access denied");
        }

        _context.CashierPlayers.Remove(assignment);
        await _context.SaveChangesAsync();

        // Auditoría
        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "UNASSIGN_PLAYER_FROM_CASHIER",
            "CashierPlayer",
            $"{cashierId}-{playerId}",
            new { CashierId = cashierId, PlayerId = playerId, CashierUsername = assignment.Cashier.Username, PlayerUsername = assignment.Player.Username });

        _logger.LogInformation("Player {PlayerUsername} unassigned from cashier {CashierUsername} by user {UserId}",
            assignment.Player.Username, assignment.Cashier.Username, currentUserId);

        return new UnassignPlayerResponse(true, "Player unassigned successfully");
    }

    // Nuevos métodos para el sistema brand-only
    public async Task<QueryPlayersResponse> GetCashierPlayersAsync(Guid cashierId, int page = 1, int pageSize = 20)
    {
        var query = _context.CashierPlayers
            .Include(cp => cp.Player)
                .ThenInclude(p => p.Brand)
            .Include(cp => cp.Player)
                .ThenInclude(p => p.Wallet)
            .Where(cp => cp.CashierId == cashierId)
            .Select(cp => cp.Player);

        var totalCount = await query.CountAsync();

        var players = await query
            .OrderBy(p => p.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var playerResponses = players.Select(p => new GetPlayerResponse(
            p.Id,
            p.BrandId,
            p.Brand.Code,
            p.Brand.Name,
            p.Username,
            p.Email,
            p.ExternalId,
            p.Status,
            p.Wallet?.BalanceBigint ?? 0,
            p.CreatedAt
        ));

        return new QueryPlayersResponse(
            playerResponses,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling((double)totalCount / pageSize)
        );
    }

    public async Task<bool> IsPlayerAssignedToCashierAsync(Guid cashierId, Guid playerId)
    {
        return await _context.CashierPlayers
            .AnyAsync(cp => cp.CashierId == cashierId && cp.PlayerId == playerId);
    }
}