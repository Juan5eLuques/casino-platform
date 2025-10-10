using Casino.Api.Utils;
using Casino.Application.DTOs.Player;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

public static class BrandOnlyPlayerEndpoints
{
    public static void MapBrandOnlyPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Players (Brand-Only)");

        // === CREATE PLAYER ===
        group.MapPost("/players", CreatePlayer)
            .RequireAuthorization("BrandScopedCashierOrAdmin")
            .WithName("CreatePlayerBrandOnly")
            .WithSummary("Create player (brand resolved by Host)")
            .WithDescription("Creates a new player in the current brand (resolved from Host)")
            .Produces<GetPlayerResponse>(201)
            .Produces(400) // brand_not_resolved, validation errors
            .Produces(403) // access denied
            .Produces(409); // username_exists

        // === LIST PLAYERS ===
        group.MapGet("/players", ListPlayers)
            .RequireAuthorization("AnyBackofficeUser") // Todos pueden listar players con su scope
            .WithName("ListPlayersBrandOnly")
            .WithSummary("List players (brand scoped)")
            .WithDescription("Lists players scoped to current brand. SUPER_ADMIN can use ?globalScope=true to see all brands. CASHIER only sees assigned players.")
            .Produces<QueryPlayersResponse>()
            .Produces(400) // brand_not_resolved
            .Produces(403); // access denied

        // === GET PLAYER ===
        group.MapGet("/players/{playerId:guid}", GetPlayer)
            .RequireAuthorization("AnyBackofficeUser")
            .WithName("GetPlayerBrandOnly")
            .WithSummary("Get player details")
            .Produces<GetPlayerResponse>()
            .Produces(404)
            .Produces(403);

        // === UPDATE PLAYER ===
        group.MapPatch("/players/{playerId:guid}", UpdatePlayer)
            .RequireAuthorization("BrandScopedCashierOrAdmin")
            .WithName("UpdatePlayerBrandOnly")
            .WithSummary("Update player information")
            .Produces<GetPlayerResponse>()
            .Produces(404)
            .Produces(403);

        // === ADJUST PLAYER WALLET ===
        group.MapPost("/players/{playerId:guid}/wallet/adjust", AdjustPlayerWallet)
            .RequireAuthorization("BrandScopedCashierOrAdmin")
            .WithName("AdjustPlayerWallet")
            .WithSummary("Adjust player wallet balance")
            .WithDescription("Add or subtract balance from player wallet with audit trail")
            .Produces<WalletAdjustmentResponse>()
            .Produces(404)
            .Produces(403)
            .Produces(400);

        // === DELETE PLAYER ===
        group.MapDelete("/players/{playerId:guid}", DeletePlayer)
            .RequireAuthorization("AdminOrSuperAdmin") // Solo admins pueden eliminar players
            .WithName("DeletePlayerBrandOnly")
            .WithSummary("Delete player")
            .Produces(200)
            .Produces(404)
            .Produces(403);
    }

    private static async Task<IResult> CreatePlayer(
        [FromBody] CreatePlayerRequest request,
        IPlayerService playerService,
        ICashierPlayerService cashierPlayerService,
        IValidator<CreatePlayerRequest> validator,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyPlayerEndpoints");

        // Validar request
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context (requerido para todas las operaciones)
            var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
            if (brandValidation != null) return brandValidation;

            // Resolver brand efectivo para el nuevo player
            var effectiveBrandId = AuthorizationHelper.GetEffectiveBrandId(
                currentRole, currentUserBrandId, brandContext);

            // Crear player (con información del rol para vincular con cashier si corresponde)
            var response = await playerService.CreatePlayerAsync(request, currentUserId, effectiveBrandId, currentRole);

            // Si es CASHIER, asignar automáticamente el player al cashier
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                try
                {
                    await cashierPlayerService.AssignPlayerAsync(currentUserId, response.Id, currentUserId);
                    logger.LogInformation("Player {PlayerId} auto-assigned to cashier {CashierId}",
                        response.Id, currentUserId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to auto-assign player {PlayerId} to cashier {CashierId}",
                        response.Id, currentUserId);
                    // No fallar la creación del player si falla la asignación
                }
            }

            logger.LogInformation("Player created: {PlayerId} by {CurrentUserId} in brand {BrandId}",
                response.Id, currentUserId, effectiveBrandId);

            return Results.Created($"/api/v1/admin/players/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Player creation failed: {Error}", ex.Message);
            
            if (ex.Message.Contains("already exists"))
                return Results.Conflict(new { error = "username_exists", message = ex.Message });
            
            return Results.Problem(
                title: "Player Creation Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating player");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> ListPlayers(
        [AsParameters] QueryPlayersRequest request,
        IPlayerService playerService,
        ICashierPlayerService cashierPlayerService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyPlayerEndpoints");

        try
        {
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar permisos para scope global (solo SUPER_ADMIN)
            if (request.GlobalScope && currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN can use global scope",
                    statusCode: 403);
            }

            // Resolver scope de consulta
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext, request.GlobalScope);

            // Validar brand context si es necesario
            if (queryScope.HasValue || (!request.GlobalScope && currentRole != BackofficeUserRole.SUPER_ADMIN))
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            QueryPlayersResponse response;

            // Para CASHIER: solo mostrar jugadores asignados a él
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                var assignedPlayers = await cashierPlayerService.GetCashierPlayersAsync(currentUserId, request.Page, request.PageSize);
                response = assignedPlayers;
                logger.LogInformation("CASHIER {CashierId} listed {Count} assigned players", currentUserId, response.TotalCount);
            }
            else
            {
                // Para SUPER_ADMIN y BRAND_ADMIN: usar el servicio normal
                response = await playerService.GetPlayersAsync(request, queryScope);
                logger.LogInformation("Listed {Count} players for role {Role} with scope {Scope}",
                    response.TotalCount, currentRole, queryScope?.ToString() ?? "global");
            }

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing players");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> GetPlayer(
        Guid playerId,
        IPlayerService playerService,
        ICashierPlayerService cashierPlayerService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyPlayerEndpoints");

        try
        {
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context
            var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
            if (brandValidation != null) return brandValidation;

            // Resolver scope
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            // Para CASHIER: verificar que el jugador esté asignado a él
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                var isAssigned = await cashierPlayerService.IsPlayerAssignedToCashierAsync(currentUserId, playerId);
                if (!isAssigned)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "CASHIER can only view assigned players",
                        statusCode: 403);
                }
            }

            var player = await playerService.GetPlayerAsync(playerId, queryScope);

            if (player == null)
            {
                return Results.NotFound(new { error = "player_not_found", playerId });
            }

            return Results.Ok(player);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting player {PlayerId}", playerId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdatePlayer(
        Guid playerId,
        [FromBody] UpdatePlayerRequest request,
        IPlayerService playerService,
        ICashierPlayerService cashierPlayerService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyPlayerEndpoints");

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context
            var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
            if (brandValidation != null) return brandValidation;

            // Para CASHIER: verificar que el jugador esté asignado a él
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                var isAssigned = await cashierPlayerService.IsPlayerAssignedToCashierAsync(currentUserId, playerId);
                if (!isAssigned)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "CASHIER can only update assigned players",
                        statusCode: 403);
                }
            }

            // Resolver scope
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var response = await playerService.UpdatePlayerAsync(playerId, request, currentUserId, queryScope);

            logger.LogInformation("Player updated: {PlayerId} by {CurrentUserId}", playerId, currentUserId);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Player update failed: {Error}", ex.Message);
            return Results.Problem(
                title: "Update Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating player {PlayerId}", playerId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> AdjustPlayerWallet(
        Guid playerId,
        [FromBody] AdjustPlayerWalletRequest request,
        IPlayerService playerService,
        ICashierPlayerService cashierPlayerService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyPlayerEndpoints");

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context
            var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
            if (brandValidation != null) return brandValidation;

            // Para CASHIER: verificar que el jugador esté asignado a él
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                var isAssigned = await cashierPlayerService.IsPlayerAssignedToCashierAsync(currentUserId, playerId);
                if (!isAssigned)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "CASHIER can only adjust wallet for assigned players",
                        statusCode: 403);
                }
            }

            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var response = await playerService.AdjustPlayerWalletAsync(
                playerId, request, currentUserId, queryScope);

            logger.LogInformation("Wallet adjusted: Player {PlayerId}, Amount {Amount}, NewBalance {NewBalance} by {CurrentUserId}",
                playerId, request.Amount, response.NewBalance, currentUserId);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Wallet adjustment failed: {Error}", ex.Message);
            return Results.Problem(
                title: "Wallet Adjustment Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adjusting wallet for player {PlayerId}", playerId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    private static async Task<IResult> DeletePlayer(
        Guid playerId,
        IPlayerService playerService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("BrandOnlyPlayerEndpoints");

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context
            var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
            if (brandValidation != null) return brandValidation;

            // Resolver scope
            var queryScope = AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var deleted = await playerService.DeletePlayerAsync(playerId, currentUserId, queryScope);

            if (!deleted)
            {
                return Results.NotFound(new { error = "player_not_found", playerId });
            }

            logger.LogInformation("Player deleted: {PlayerId} by {CurrentUserId}", playerId, currentUserId);

            return Results.Ok(new { success = true, message = "Player deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Player deletion failed: {Error}", ex.Message);
            return Results.Problem(
                title: "Deletion Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting player {PlayerId}", playerId);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }
}