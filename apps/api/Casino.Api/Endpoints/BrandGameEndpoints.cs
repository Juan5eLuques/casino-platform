using Casino.Application.DTOs.BrandGame;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class BrandGameEndpoints
{
    public static void MapBrandGameEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/brands/{brandId:guid}/games", AssignGameToBrand)
            .WithName("AssignGameToBrand")
            .WithSummary("Assign a game to a brand")
            .WithTags("Brand Game Management")
            .Produces<BrandGameResponse>(201)
            .Produces(400)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapGet("/brands/{brandId:guid}/games", GetBrandGames)
            .WithName("GetBrandGamesAdmin")
            .WithSummary("Get games assigned to a brand")
            .WithTags("Brand Game Management")
            .Produces<GetBrandGamesResponse>()
            .Produces(404);

        app.MapPatch("/brands/{brandId:guid}/games/{gameId:guid}", UpdateBrandGame)
            .WithName("UpdateBrandGame")
            .WithSummary("Update brand game configuration")
            .WithTags("Brand Game Management")
            .Produces<BrandGameResponse>()
            .Produces(404)
            .ProducesValidationProblem();

        app.MapDelete("/brands/{brandId:guid}/games/{gameId:guid}", RemoveGameFromBrand)
            .WithName("RemoveGameFromBrand")
            .WithSummary("Remove a game from a brand")
            .WithTags("Brand Game Management")
            .Produces(200)
            .Produces(404)
            .Produces(409);
    }

    private static async Task<IResult> AssignGameToBrand(
        Guid brandId,
        [FromBody] AssignGameToBrandRequest request,
        IBrandGameService brandGameService,
        IValidator<AssignGameToBrandRequest> validator,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = GetCurrentUserId(httpContext);
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            // Solo SUPER_ADMIN y OPERATOR_ADMIN pueden asignar juegos
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot assign games to brands",
                    statusCode: 403);
            }

            var response = await brandGameService.AssignGameToBrandAsync(brandId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Game assigned to brand: {GameId} to {BrandId} by user {UserId}",
                request.GameId, brandId, currentUserId);
            
            return TypedResults.Created($"/api/v1/admin/brands/{brandId}/games/{request.GameId}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Game assignment failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Game Assignment Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning game {GameId} to brand {BrandId}", request.GameId, brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while assigning game to brand",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrandGames(
        Guid brandId,
        IBrandGameService brandGameService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            var response = await brandGameService.GetBrandGamesAsync(brandId, operatorScope);
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand games access failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Not Found",
                detail: ex.Message,
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting brand games for brand {BrandId}", brandId);
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
        IBrandGameService brandGameService,
        IValidator<UpdateBrandGameRequest> validator,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = GetCurrentUserId(httpContext);
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            // Solo SUPER_ADMIN y OPERATOR_ADMIN pueden actualizar configuración de juegos
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot update brand game configuration",
                    statusCode: 403);
            }

            var response = await brandGameService.UpdateBrandGameAsync(brandId, gameId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Brand game updated: {GameId} in {BrandId} by user {UserId}",
                gameId, brandId, currentUserId);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand game update failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Game Update Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating brand game {GameId} in brand {BrandId}", gameId, brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating brand game",
                statusCode: 500);
        }
    }

    private static async Task<IResult> RemoveGameFromBrand(
        Guid brandId,
        Guid gameId,
        IBrandGameService brandGameService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentUserId = GetCurrentUserId(httpContext);
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            // Solo SUPER_ADMIN y OPERATOR_ADMIN pueden remover juegos
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot remove games from brands",
                    statusCode: 403);
            }

            var success = await brandGameService.RemoveGameFromBrandAsync(brandId, gameId, currentUserId, operatorScope);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Game Not Found",
                    detail: "Game is not assigned to this brand or access denied",
                    statusCode: 404);
            }

            logger.LogInformation("Game removed from brand: {GameId} from {BrandId} by user {UserId}",
                gameId, brandId, currentUserId);
            
            return TypedResults.Ok(new { Success = true, Message = "Game removed from brand successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Game removal failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Game Removal Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing game {GameId} from brand {BrandId}", gameId, brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while removing game from brand",
                statusCode: 500);
        }
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("Invalid user ID in token");
        }
        return userId;
    }

    private static BackofficeUserRole GetCurrentUserRole(HttpContext httpContext)
    {
        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<BackofficeUserRole>(roleClaim, out var role))
        {
            throw new InvalidOperationException("Invalid role in token");
        }
        return role;
    }

    private static Guid? GetOperatorScope(HttpContext httpContext, BackofficeUserRole role)
    {
        if (role == BackofficeUserRole.SUPER_ADMIN)
            return null; // SUPER_ADMIN ve todas las marcas

        var operatorIdClaim = httpContext.User.FindFirst("operator_id")?.Value;
        if (Guid.TryParse(operatorIdClaim, out var operatorId))
            return operatorId;

        return null;
    }
}