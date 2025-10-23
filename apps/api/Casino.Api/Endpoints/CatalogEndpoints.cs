using Casino.Application.DTOs.Game;
using Casino.Application.DTOs.Session;
using Casino.Application.Services;
using Casino.Application.Services.Models;
using Casino.Application.Mappers;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casino.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog")
            .WithTags("Catalog");

        group.MapGet("/games", GetCatalogGames)
            .WithName("GetCatalogGames")
            .WithSummary("Get games available for current brand")
            .Produces<IEnumerable<CatalogGameResponse>>();

        group.MapPost("/games/{gameCode}/launch", LaunchGame)
            .WithName("LaunchGame")
            .WithSummary("Launch a game for a player")
            .Produces<LaunchGameResponse>()
            .Produces(400)
            .Produces(404);
    }

    public record LaunchGameRequest(
        Guid PlayerId,
        int ExpirationMinutes = 60);

    public record LaunchGameResponse(
        Guid SessionId,
        string GameCode,
        string GameUrl,
        DateTime ExpiresAt);

    private static async Task<IResult> GetCatalogGames(
        BrandContext brandContext,
        IBrandService brandService,
        [FromQuery] string? category = null,
        [FromQuery] string? provider = null,
        [FromQuery] string? type = null,
        [FromQuery] bool? featured = null,
        [FromQuery] bool? enabled = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        ILogger<Program> logger = null!)
    {
        try
        {
            if (!brandContext.IsResolved)
            {
                return Results.Problem(
                    title: "Brand Not Resolved",
                    detail: "Brand context is not available",
                    statusCode: 400);
            }

            logger?.LogInformation("Getting catalog games for brand: {BrandCode} ({BrandId})", 
                brandContext.BrandCode, brandContext.BrandId);

            // Usar el servicio para obtener los juegos
            var gamesResult = await brandService.GetBrandCatalogAsync(brandContext.BrandId);
       
    // Aplicar filtros en memoria (ya que GetBrandCatalogAsync devuelve IEnumerable)
            if (!string.IsNullOrEmpty(category))
    gamesResult = gamesResult.Where(g => g.Category == category);

      if (!string.IsNullOrEmpty(provider))
   gamesResult = gamesResult.Where(g => g.Provider == provider);

            // ? NEW: Filter by Type
       if (!string.IsNullOrEmpty(type) && Enum.TryParse<Casino.Domain.Enums.GameType>(type, true, out var gameType))
         gamesResult = gamesResult.Where(g => g.Type == gameType);

     if (featured.HasValue)
    gamesResult = gamesResult.Where(g => g.IsFeatured == featured.Value);

    if (enabled.HasValue)
   gamesResult = gamesResult.Where(g => g.Enabled == enabled.Value);

 // Materializar la lista para poder contar y paginar
  var gamesList = gamesResult.ToList();
         var totalCount = gamesList.Count;

        // Aplicar paginación
    var paginatedGames = gamesList
 .Skip((page - 1) * pageSize)
   .Take(pageSize)
   .ToList();

       // Mapear a DTOs con todos los campos extendidos
   var games = paginatedGames.Select(g => new CatalogGameResponse(
    g.GameId,
                g.Code,
   g.Name,
        g.Provider,
   g.Type.ToString(),  // ? Convert enum to string
    g.Category,
        g.ImageUrl,
   g.RTP,
        g.Volatility,
g.MinBet,
   g.MaxBet,
   g.IsFeatured,
    g.IsNew,
   g.Enabled,
      g.DisplayOrder,
  g.Tags
           )).ToList();

          // Response con paginación
            var response = new CatalogGamesResponse(
 games,
       page,
 pageSize,
     totalCount
     );

    logger?.LogInformation("Retrieved {Count}/{Total} games for brand {BrandCode} (page {Page})", 
    games.Count, totalCount, brandContext.BrandCode, page);

     return TypedResults.Ok(response);
 }
        catch (Exception ex)
        {
     logger?.LogError(ex, "Error getting catalog games for brand: {BrandCode}", brandContext.BrandCode);
     return Results.Problem(
        title: "Internal Server Error",
      detail: "An error occurred while getting catalog games",
    statusCode: 500);
     }
    }

    private static async Task<IResult> LaunchGame(
        string gameCode,
        [FromBody] LaunchGameRequest request,
        BrandContext brandContext,
        ISessionService sessionService,
        CasinoDbContext context,
        ILogger<Program> logger)
    {
        try
        {
            if (!brandContext.IsResolved)
            {
                return Results.Problem(
                    title: "Brand Not Resolved",
                    detail: "Brand context is not available",
                    statusCode: 400);
            }

            logger.LogInformation("Launching game {GameCode} for player {PlayerId} in brand {BrandCode}", 
                gameCode, request.PlayerId, brandContext.BrandCode);

            // Verify the game is available for this brand
            var brandGame = await context.BrandGames
                .Include(bg => bg.Game)
                .FirstOrDefaultAsync(bg => 
                    bg.BrandId == brandContext.BrandId && 
                    bg.Game.Code == gameCode && 
                    bg.Enabled);

            if (brandGame == null)
            {
                logger.LogWarning("Game {GameCode} not found or not enabled for brand {BrandCode}", 
                    gameCode, brandContext.BrandCode);
                
                return Results.Problem(
                    title: "Game Not Available",
                    detail: "Game is not available for this brand",
                    statusCode: 404);
            }

            // Verify player belongs to this brand
            var player = await context.Players
                .FirstOrDefaultAsync(p => 
                    p.Id == request.PlayerId && 
                    p.BrandId == brandContext.BrandId &&
                    p.Status == PlayerStatus.ACTIVE);

            if (player == null)
            {
                logger.LogWarning("Player {PlayerId} not found or not active for brand {BrandCode}", 
                    request.PlayerId, brandContext.BrandCode);
                
                return Results.Problem(
                    title: "Player Not Found",
                    detail: "Player not found or not active for this brand",
                    statusCode: 404);
            }

            // Create game session
            var sessionRequest = new CreateSessionRequest(
                request.PlayerId,
                gameCode,
                brandGame.Game.Provider,
                request.ExpirationMinutes);

            var sessionResponse = await sessionService.CreateSessionAsync(sessionRequest);

            // Generate game URL (this would typically point to your game loader)
            var gameUrl = $"/games/{gameCode}?session={sessionResponse.SessionId}";

            var launchResponse = new LaunchGameResponse(
                sessionResponse.SessionId,
                gameCode,
                gameUrl,
                sessionResponse.ExpiresAt);

            logger.LogInformation("Game launched successfully: {GameCode} for player {PlayerId}, session {SessionId}", 
                gameCode, request.PlayerId, sessionResponse.SessionId);

            return TypedResults.Ok(launchResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error launching game {GameCode} for player {PlayerId}", gameCode, request.PlayerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while launching game",
                statusCode: 500);
        }
    }
}