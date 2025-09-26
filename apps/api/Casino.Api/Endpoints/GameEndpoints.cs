using Casino.Application.DTOs.Game;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var gameGroup = app.MapGroup("/api/v1/admin/games")
            .WithTags("Game Management");

        var catalogGroup = app.MapGroup("/api/v1/admin/catalog")
            .WithTags("Game Catalog");

        // Game management endpoints
        gameGroup.MapPost("/", CreateGame)
            .WithName("AdminCreateGame")
            .WithSummary("Create a new game")
            .Produces<CreateGameResponse>()
            .ProducesValidationProblem();

        gameGroup.MapGet("/", GetGames)
            .WithName("AdminGetGames")
            .WithSummary("Get all games")
            .Produces<IEnumerable<GetGameResponse>>();

        gameGroup.MapGet("/{gameId:guid}", GetGame)
            .WithName("AdminGetGame")
            .WithSummary("Get game by ID")
            .Produces<GetGameResponse>()
            .Produces(404);

        gameGroup.MapPatch("/{gameId:guid}", UpdateGame)
            .WithName("AdminUpdateGame")
            .WithSummary("Update game")
            .Produces<GetGameResponse>()
            .Produces(404);

        gameGroup.MapDelete("/{gameId:guid}", DeleteGame)
            .WithName("AdminDeleteGame")
            .WithSummary("Delete game")
            .Produces(200)
            .Produces(404);

        // Brand-Game catalog management
        catalogGroup.MapPost("/brands/{brandId:guid}/games/{gameId:guid}", AssignGameToBrand)
            .WithName("AdminAssignGameToBrand")
            .WithSummary("Assign game to brand")
            .Produces(201)
            .Produces(404)
            .Produces(409);

        catalogGroup.MapDelete("/brands/{brandId:guid}/games/{gameId:guid}", UnassignGameFromBrand)
            .WithName("AdminUnassignGameFromBrand")
            .WithSummary("Unassign game from brand")
            .Produces(200)
            .Produces(404);

        catalogGroup.MapGet("/brands/{brandId:guid}/games", GetBrandGames)
            .WithName("AdminGetBrandGames")
            .WithSummary("Get games assigned to brand")
            .Produces<IEnumerable<GetBrandGameResponse>>();

        catalogGroup.MapPatch("/brands/{brandId:guid}/games/{gameId:guid}", UpdateBrandGame)
            .WithName("AdminUpdateBrandGame")
            .WithSummary("Update brand-game assignment")
            .Produces(200)
            .Produces(404);
    }

    private static async Task<IResult> CreateGame(
        [FromBody] CreateGameRequest request,
        IGameService gameService,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Creating game: {Code} by provider {Provider}", request.Code, request.Provider);
            
            var response = await gameService.CreateGameAsync(request);
            return TypedResults.Created($"/api/v1/admin/games/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Game creation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Game Creation Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating game: {Code}", request.Code);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating game",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetGames(
        IGameService gameService,
        bool? enabled = null)
    {
        try
        {
            var games = await gameService.GetGamesAsync(enabled);
            return TypedResults.Ok(games);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting games",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetGame(
        Guid gameId,
        IGameService gameService)
    {
        try
        {
            var game = await gameService.GetGameAsync(gameId);
            
            if (game == null)
            {
                return Results.Problem(
                    title: "Game Not Found",
                    detail: "Game does not exist",
                    statusCode: 404);
            }

            return TypedResults.Ok(game);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting game",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateGame(
        Guid gameId,
        [FromBody] UpdateGameRequest request,
        IGameService gameService,
        ILogger<Program> logger)
    {
        try
        {
            var success = await gameService.UpdateGameAsync(gameId, request);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Game Not Found",
                    detail: "Game does not exist",
                    statusCode: 404);
            }

            var updatedGame = await gameService.GetGameAsync(gameId);
            return TypedResults.Ok(updatedGame);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating game: {GameId}", gameId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating game",
                statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteGame(
        Guid gameId,
        IGameService gameService,
        ILogger<Program> logger)
    {
        try
        {
            var success = await gameService.DeleteGameAsync(gameId);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Game Not Found",
                    detail: "Game does not exist",
                    statusCode: 404);
            }

            return TypedResults.Ok(new { Success = true, Message = "Game deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Game deletion failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Game Deletion Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting game: {GameId}", gameId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while deleting game",
                statusCode: 500);
        }
    }

    private static async Task<IResult> AssignGameToBrand(
        Guid brandId,
        Guid gameId,
        [FromBody] AssignGameToBrandRequest? request,
        IGameService gameService,
        ILogger<Program> logger)
    {
        try
        {
            // Use route parameters if no body is provided
            var assignRequest = request ?? new AssignGameToBrandRequest(brandId, gameId);
            
            // Override with route parameters to ensure consistency
            assignRequest = assignRequest with { BrandId = brandId, GameId = gameId };
            
            var success = await gameService.AssignGameToBrandAsync(assignRequest);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Brand or Game Not Found",
                    detail: "Brand or game does not exist",
                    statusCode: 404);
            }

            return TypedResults.Created($"/api/v1/admin/catalog/brands/{brandId}/games/{gameId}", 
                new { Success = true, Message = "Game assigned to brand successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Game assignment failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Assignment Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning game {GameId} to brand {BrandId}", gameId, brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while assigning game to brand",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UnassignGameFromBrand(
        Guid brandId,
        Guid gameId,
        IGameService gameService,
        ILogger<Program> logger)
    {
        try
        {
            var success = await gameService.UnassignGameFromBrandAsync(brandId, gameId);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Assignment Not Found",
                    detail: "Game is not assigned to this brand",
                    statusCode: 404);
            }

            return TypedResults.Ok(new { Success = true, Message = "Game unassigned from brand successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unassigning game {GameId} from brand {BrandId}", gameId, brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while unassigning game from brand",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrandGames(
        Guid brandId,
        IGameService gameService,
        bool? enabled = null)
    {
        try
        {
            var games = await gameService.GetBrandGamesAsync(brandId, enabled);
            return TypedResults.Ok(games);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brand games",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateBrandGame(
        Guid brandId,
        Guid gameId,
        [FromBody] UpdateBrandGameRequest request,
        IGameService gameService,
        ILogger<Program> logger)
    {
        try
        {
            // Override with route parameters to ensure consistency
            var updateRequest = request with { BrandId = brandId, GameId = gameId };
            
            var success = await gameService.UpdateBrandGameAsync(updateRequest);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Assignment Not Found",
                    detail: "Game is not assigned to this brand",
                    statusCode: 404);
            }

            return TypedResults.Ok(new { Success = true, Message = "Brand game assignment updated successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating brand game {BrandId}/{GameId}", brandId, gameId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating brand game assignment",
                statusCode: 500);
        }
    }
}