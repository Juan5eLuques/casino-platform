using Casino.Application.DTOs.BrandGame;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class BrandGameService : IBrandGameService
{
    private readonly CasinoDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<BrandGameService> _logger;

    public BrandGameService(
        CasinoDbContext context,
        IAuditService auditService,
        ILogger<BrandGameService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<BrandGameResponse> AssignGameToBrandAsync(Guid brandId, AssignGameToBrandRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        // Verificar que la marca existe y el usuario tiene acceso
        var brand = await _context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId && 
                (brandScope == null || b.Id == brandScope));

        if (brand == null)
        {
            throw new InvalidOperationException("Brand not found or access denied");
        }

        // Verificar que el juego existe
        var game = await _context.Games
            .FirstOrDefaultAsync(g => g.Id == request.GameId && g.Enabled);

        if (game == null)
        {
            throw new InvalidOperationException("Game not found or disabled");
        }

        // Verificar si ya está asignado
        var existingAssignment = await _context.BrandGames
            .FirstOrDefaultAsync(bg => bg.BrandId == brandId && bg.GameId == request.GameId);

        if (existingAssignment != null)
        {
            throw new InvalidOperationException("Game is already assigned to this brand");
        }

        var brandGame = new BrandGame
        {
            BrandId = brandId,
            GameId = request.GameId,
            Enabled = request.Enabled,
            DisplayOrder = request.DisplayOrder,
            Tags = request.Tags ?? Array.Empty<string>()
        };

        _context.BrandGames.Add(brandGame);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "ASSIGN_GAME_TO_BRAND",
            "BrandGame",
            brandId.ToString(),
            new { 
                GameId = request.GameId,
                GameCode = game.Code,
                request.Enabled,
                request.DisplayOrder,
                Tags = request.Tags
            });

        _logger.LogInformation("Game assigned to brand: {GameId} to {BrandId} by user {UserId}",
            request.GameId, brandId, currentUserId);

        return new BrandGameResponse(
            game.Id,
            game.Code,
            game.Name,
            game.Provider,
            brandGame.Enabled,
            brandGame.DisplayOrder,
            brandGame.Tags);
    }

    public async Task<GetBrandGamesResponse> GetBrandGamesAsync(Guid brandId, Guid? brandScope = null)
    {
        // Verificar que la marca existe y el usuario tiene acceso
        var brand = await _context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId && 
                (brandScope == null || b.Id == brandScope));

        if (brand == null)
        {
            throw new InvalidOperationException("Brand not found or access denied");
        }

        var brandGames = await _context.BrandGames
            .Include(bg => bg.Game)
            .Where(bg => bg.BrandId == brandId)
            .OrderBy(bg => bg.DisplayOrder)
            .ThenBy(bg => bg.Game.Name)
            .Select(bg => new BrandGameResponse(
                bg.Game.Id,
                bg.Game.Code,
                bg.Game.Name,
                bg.Game.Provider,
                bg.Enabled,
                bg.DisplayOrder,
                bg.Tags))
            .ToListAsync();

        return new GetBrandGamesResponse(
            brandId,
            brand.Code,
            brandGames);
    }

    public async Task<BrandGameResponse> UpdateBrandGameAsync(Guid brandId, Guid gameId, UpdateBrandGameRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        // Verificar que la marca existe y el usuario tiene acceso
        var brand = await _context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId && 
                (brandScope == null || b.Id == brandScope));

        if (brand == null)
        {
            throw new InvalidOperationException("Brand not found or access denied");
        }

        var brandGame = await _context.BrandGames
            .Include(bg => bg.Game)
            .FirstOrDefaultAsync(bg => bg.BrandId == brandId && bg.GameId == gameId);

        if (brandGame == null)
        {
            throw new InvalidOperationException("Game is not assigned to this brand");
        }

        var changes = new Dictionary<string, object>();

        if (request.Enabled.HasValue && request.Enabled.Value != brandGame.Enabled)
        {
            changes["Enabled"] = new { Old = brandGame.Enabled, New = request.Enabled.Value };
            brandGame.Enabled = request.Enabled.Value;
        }

        if (request.DisplayOrder.HasValue && request.DisplayOrder.Value != brandGame.DisplayOrder)
        {
            changes["DisplayOrder"] = new { Old = brandGame.DisplayOrder, New = request.DisplayOrder.Value };
            brandGame.DisplayOrder = request.DisplayOrder.Value;
        }

        if (request.Tags != null && !request.Tags.SequenceEqual(brandGame.Tags))
        {
            changes["Tags"] = new { Old = brandGame.Tags, New = request.Tags };
            brandGame.Tags = request.Tags;
        }

        if (changes.Any())
        {
            await _context.SaveChangesAsync();

            await _auditService.LogBackofficeActionAsync(
                currentUserId,
                "UPDATE_BRAND_GAME",
                "BrandGame",
                brandId.ToString(),
                new { 
                    GameId = gameId,
                    GameCode = brandGame.Game.Code,
                    Changes = changes
                });

            _logger.LogInformation("Brand game updated: {GameId} in {BrandId} by user {UserId}",
                gameId, brandId, currentUserId);
        }

        return new BrandGameResponse(
            brandGame.Game.Id,
            brandGame.Game.Code,
            brandGame.Game.Name,
            brandGame.Game.Provider,
            brandGame.Enabled,
            brandGame.DisplayOrder,
            brandGame.Tags);
    }

    public async Task<bool> RemoveGameFromBrandAsync(Guid brandId, Guid gameId, Guid currentUserId, Guid? brandScope = null)
    {
        // Verificar que la marca existe y el usuario tiene acceso
        var brand = await _context.Brands
            .FirstOrDefaultAsync(b => b.Id == brandId && 
                (brandScope == null || b.Id == brandScope));

        if (brand == null)
        {
            throw new InvalidOperationException("Brand not found or access denied");
        }

        var brandGame = await _context.BrandGames
            .Include(bg => bg.Game)
            .FirstOrDefaultAsync(bg => bg.BrandId == brandId && bg.GameId == gameId);

        if (brandGame == null)
            return false;

        // Verificar si hay sesiones activas para este juego en esta marca
        var activeSessions = await _context.GameSessions
            .AnyAsync(gs => gs.Player.BrandId == brandId && 
                           gs.GameCode == brandGame.Game.Code && 
                           gs.Status == GameSessionStatus.OPEN);

        if (activeSessions)
        {
            throw new InvalidOperationException("Cannot remove game with active sessions. Close all sessions first.");
        }

        _context.BrandGames.Remove(brandGame);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(
            currentUserId,
            "REMOVE_GAME_FROM_BRAND",
            "BrandGame",
            brandId.ToString(),
            new { 
                GameId = gameId,
                GameCode = brandGame.Game.Code,
                GameName = brandGame.Game.Name,
                RemovedAt = DateTime.UtcNow
            });

        _logger.LogInformation("Game removed from brand: {GameId} from {BrandId} by user {UserId}",
            gameId, brandId, currentUserId);

        return true;
    }
}