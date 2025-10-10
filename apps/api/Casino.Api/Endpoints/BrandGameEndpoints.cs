using Casino.Api.Utils;
using Casino.Application.DTOs.BrandGame;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

public static class BrandGameEndpoints
{
    public static void MapBrandGameEndpoints(this IEndpointRouteBuilder app)
    {
        // SONNET: Eliminar {brandId} de la ruta ya que usamos BrandContext
        app.MapPost("/brands/games", AssignGameToBrand)
            .WithName("AssignGameToBrand")
            .WithSummary("Assign a game to current brand")
            .WithTags("Brand Game Management")
            .Produces<BrandGameResponse>(201)
            .Produces(400)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapGet("/brands/games", GetBrandGames)
            .WithName("GetBrandGamesAdmin")
            .WithSummary("Get games assigned to current brand")
            .WithTags("Brand Game Management")
            .Produces<GetBrandGamesResponse>()
            .Produces(404);

        app.MapPatch("/brands/games/{gameId:guid}", UpdateBrandGame)
            .WithName("UpdateBrandGame")
            .WithSummary("Update brand game configuration")
            .WithTags("Brand Game Management")
            .Produces<BrandGameResponse>()
            .Produces(404)
            .ProducesValidationProblem();

        app.MapDelete("/brands/games/{gameId:guid}", RemoveGameFromBrand)
            .WithName("RemoveGameFromBrand")
            .WithSummary("Remove a game from current brand")
            .WithTags("Brand Game Management")
            .Produces(200)
            .Produces(404)
            .Produces(409);
    }

    // SONNET: Corregido parámetros con [FromServices] y usar BrandContext.BrandId
    private static async Task<IResult> AssignGameToBrand(
        [FromBody] AssignGameToBrandRequest request,
        [FromServices] IBrandGameService brandGameService,
        [FromServices] IValidator<AssignGameToBrandRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            // SONNET: Validar que el brand esté resuelto
            if (!brandContext.IsResolved)
            {
                return Results.BadRequest(new { error = "brand_not_resolved", message = "Brand context could not be resolved from host" });
            }

            var brandId = brandContext.BrandId; // SONNET: Usar BrandContext en lugar de parámetro

            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar permisos usando el método correcto
            var permissionError = AuthorizationHelper.ValidateUserOperationPermissions(currentRole);
            if (permissionError != null)
                return permissionError;

            // Validar que solo SUPER_ADMIN o BRAND_ADMIN pueden asignar juegos
            if (currentRole != BackofficeUserRole.SUPER_ADMIN && currentRole != BackofficeUserRole.BRAND_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN or BRAND_ADMIN can assign games to brands",
                    statusCode: 403);
            }

            var brandScope = AuthorizationHelper.GetQueryScope(currentRole, currentUserBrandId, brandContext);

            var response = await brandGameService.AssignGameToBrandAsync(brandId, request, currentUserId, brandScope);
            
            logger.LogInformation("Game assigned to brand: {GameId} to {BrandId} by {AuthContext}",
                request.GameId, brandId, AuthorizationHelper.GetAuthorizationContext(httpContext, brandContext));
            
            return TypedResults.Created($"/api/v1/admin/brands/games/{request.GameId}", response);
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
            logger.LogError(ex, "Error assigning game {GameId} to brand", request.GameId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while assigning game to brand",
                statusCode: 500);
        }
    }

    // SONNET: Corregido parámetros con [FromServices]
    private static async Task<IResult> GetBrandGames(
        [FromServices] IBrandGameService brandGameService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            // SONNET: Validar que el brand esté resuelto
            if (!brandContext.IsResolved)
            {
                return Results.BadRequest(new { error = "brand_not_resolved", message = "Brand context could not be resolved from host" });
            }

            var brandId = brandContext.BrandId; // SONNET: Usar BrandContext

            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);
            var brandScope = AuthorizationHelper.GetQueryScope(currentRole, currentUserBrandId, brandContext);

            var response = await brandGameService.GetBrandGamesAsync(brandId, brandScope);
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
            logger.LogError(ex, "Error getting brand games");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brand games",
                statusCode: 500);
        }
    }

    // SONNET: Corregido parámetros con [FromServices]
    private static async Task<IResult> UpdateBrandGame(
        Guid gameId,
        [FromBody] UpdateBrandGameRequest request,
        [FromServices] IBrandGameService brandGameService,
        [FromServices] IValidator<UpdateBrandGameRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            // SONNET: Validar que el brand esté resuelto
            if (!brandContext.IsResolved)
            {
                return Results.BadRequest(new { error = "brand_not_resolved", message = "Brand context could not be resolved from host" });
            }

            var brandId = brandContext.BrandId; // SONNET: Usar BrandContext

            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar permisos usando el método correcto
            var permissionError = AuthorizationHelper.ValidateUserOperationPermissions(currentRole);
            if (permissionError != null)
                return permissionError;

            // Validar que solo SUPER_ADMIN o BRAND_ADMIN pueden actualizar configuración de juegos
            if (currentRole != BackofficeUserRole.SUPER_ADMIN && currentRole != BackofficeUserRole.BRAND_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN or BRAND_ADMIN can update brand game configuration",
                    statusCode: 403);
            }

            var brandScope = AuthorizationHelper.GetQueryScope(currentRole, currentUserBrandId, brandContext);

            var response = await brandGameService.UpdateBrandGameAsync(brandId, gameId, request, currentUserId, brandScope);
            
            logger.LogInformation("Brand game updated: {GameId} in {BrandId} by {AuthContext}",
                gameId, brandId, AuthorizationHelper.GetAuthorizationContext(httpContext, brandContext));
            
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
            logger.LogError(ex, "Error updating brand game {GameId}", gameId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating brand game",
                statusCode: 500);
        }
    }

    // SONNET: Corregido parámetros con [FromServices]
    private static async Task<IResult> RemoveGameFromBrand(
        Guid gameId,
        [FromServices] IBrandGameService brandGameService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        try
        {
            // SONNET: Validar que el brand esté resuelto
            if (!brandContext.IsResolved)
            {
                return Results.BadRequest(new { error = "brand_not_resolved", message = "Brand context could not be resolved from host" });
            }

            var brandId = brandContext.BrandId; // SONNET: Usar BrandContext

            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar permisos usando el método correcto
            var permissionError = AuthorizationHelper.ValidateUserOperationPermissions(currentRole);
            if (permissionError != null)
                return permissionError;

            // Validar que solo SUPER_ADMIN o BRAND_ADMIN pueden remover juegos
            if (currentRole != BackofficeUserRole.SUPER_ADMIN && currentRole != BackofficeUserRole.BRAND_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN or BRAND_ADMIN can remove games from brands",
                    statusCode: 403);
            }

            var brandScope = AuthorizationHelper.GetQueryScope(currentRole, currentUserBrandId, brandContext);

            var success = await brandGameService.RemoveGameFromBrandAsync(brandId, gameId, currentUserId, brandScope);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Game Not Found",
                    detail: "Game is not assigned to this brand or access denied",
                    statusCode: 404);
            }

            logger.LogInformation("Game removed from brand: {GameId} from {BrandId} by {AuthContext}",
                gameId, brandId, AuthorizationHelper.GetAuthorizationContext(httpContext, brandContext));
            
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
            logger.LogError(ex, "Error removing game {GameId} from brand", gameId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while removing game from brand",
                statusCode: 500);
        }
    }
}