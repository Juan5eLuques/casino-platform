using Casino.Application.DTOs.Game;
using Casino.Application.Services;
using Casino.Application.Services.Models;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class GameService : IGameService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<GameService> _logger;

    public GameService(CasinoDbContext context, ILogger<GameService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request)
    {
        try
        {
            // Check if game code already exists
            var existingGame = await _context.Games
                .FirstOrDefaultAsync(g => g.Code == request.Code);

            if (existingGame != null)
            {
                throw new InvalidOperationException($"Game with code '{request.Code}' already exists");
            }

            var game = new Game
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Provider = request.Provider,
                Name = request.Name,
                Enabled = request.Enabled,
                CreatedAt = DateTime.UtcNow
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Game created: {Code} by provider {Provider}", request.Code, request.Provider);

            return new CreateGameResponse(
                game.Id,
                game.Code,
                game.Provider,
                game.Name,
                game.Enabled,
                game.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game: {Code}", request.Code);
            throw;
        }
    }

    public async Task<IEnumerable<GetGameResponse>> GetGamesAsync(bool? enabled = null)
    {
        var query = _context.Games.AsNoTracking();

        if (enabled.HasValue)
            query = query.Where(g => g.Enabled == enabled.Value);

        var games = await query
            .OrderBy(g => g.Name)
            .ToListAsync();

        return games.Select(g => new GetGameResponse(
            g.Id,
            g.Code,
            g.Provider,
            g.Name,
            g.Enabled,
            g.CreatedAt));
    }

    public async Task<GetGameResponse?> GetGameAsync(Guid gameId)
    {
        var game = await _context.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game == null)
            return null;

        return new GetGameResponse(
            game.Id,
            game.Code,
            game.Provider,
            game.Name,
            game.Enabled,
            game.CreatedAt);
    }

    public async Task<bool> UpdateGameAsync(Guid gameId, UpdateGameRequest request)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null)
            return false;

        if (!string.IsNullOrEmpty(request.Name))
            game.Name = request.Name;

        if (request.Enabled.HasValue)
            game.Enabled = request.Enabled.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Game updated: {GameId}", gameId);
        return true;
    }

    public async Task<bool> DeleteGameAsync(Guid gameId)
    {
        var game = await _context.Games.FindAsync(gameId);
        if (game == null)
            return false;

        // Check if game is assigned to any brands
        var brandAssignments = await _context.BrandGames
            .Where(bg => bg.GameId == gameId)
            .CountAsync();

        if (brandAssignments > 0)
        {
            throw new InvalidOperationException("Cannot delete game that is assigned to brands. Remove brand assignments first.");
        }

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Game deleted: {GameId}", gameId);
        return true;
    }

    public async Task<bool> AssignGameToBrandAsync(AssignGameToBrandRequest request)
    {
        // Verify brand and game exist
        var brand = await _context.Brands.FindAsync(request.BrandId);
        var game = await _context.Games.FindAsync(request.GameId);

        if (brand == null || game == null)
            return false;

        // Check if assignment already exists
        var existingAssignment = await _context.BrandGames
            .FirstOrDefaultAsync(bg => bg.BrandId == request.BrandId && bg.GameId == request.GameId);

        if (existingAssignment != null)
        {
            throw new InvalidOperationException("Game is already assigned to this brand");
        }

        var brandGame = new BrandGame
        {
            BrandId = request.BrandId,
            GameId = request.GameId,
            Enabled = request.Enabled,
            DisplayOrder = request.DisplayOrder,
            Tags = request.Tags ?? Array.Empty<string>()
        };

        _context.BrandGames.Add(brandGame);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Game {GameId} assigned to brand {BrandId}", request.GameId, request.BrandId);
        return true;
    }

    public async Task<bool> UnassignGameFromBrandAsync(Guid brandId, Guid gameId)
    {
        var assignment = await _context.BrandGames
            .FirstOrDefaultAsync(bg => bg.BrandId == brandId && bg.GameId == gameId);

        if (assignment == null)
            return false;

        _context.BrandGames.Remove(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Game {GameId} unassigned from brand {BrandId}", gameId, brandId);
        return true;
    }

    public async Task<IEnumerable<GetBrandGameResult>> GetBrandGamesAsync(Guid brandId, bool? enabled = null)
    {
        var query = _context.BrandGames
            .Include(bg => bg.Game)
            .Where(bg => bg.BrandId == brandId)
            .AsNoTracking();

        if (enabled.HasValue)
            query = query.Where(bg => bg.Enabled == enabled.Value);

        var brandGames = await query
            .OrderBy(bg => bg.DisplayOrder)
            .ThenBy(bg => bg.Game.Name)
            .ToListAsync();

        return brandGames.Select(bg => new GetBrandGameResult(
            bg.GameId,
            bg.Game.Code,
            bg.Game.Name,
            bg.Game.Provider,
            bg.Enabled,
            bg.DisplayOrder,
            bg.Tags));
    }

    public async Task<bool> UpdateBrandGameAsync(UpdateBrandGameRequest request)
    {
        var brandGame = await _context.BrandGames
            .FirstOrDefaultAsync(bg => bg.BrandId == request.BrandId && bg.GameId == request.GameId);

        if (brandGame == null)
            return false;

        if (request.Enabled.HasValue)
            brandGame.Enabled = request.Enabled.Value;

        if (request.DisplayOrder.HasValue)
            brandGame.DisplayOrder = request.DisplayOrder.Value;

        if (request.Tags != null)
            brandGame.Tags = request.Tags;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Brand game updated: {BrandId}/{GameId}", request.BrandId, request.GameId);
        return true;
    }
}