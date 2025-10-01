using Casino.Application.DTOs.Player;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class PlayerManagementEndpoints
{
    public static void MapPlayerManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players", CreatePlayer)
            .WithName("CreatePlayer")
            .WithSummary("Create a new player")
            .WithTags("Player Management")
            .Produces<GetPlayerResponse>(201)
            .Produces(400)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapGet("/players", GetPlayers)
            .WithName("GetPlayersAdmin")
            .WithSummary("Get players with filtering and pagination")
            .WithTags("Player Management")
            .Produces<QueryPlayersResponse>();

        app.MapGet("/players/{playerId:guid}", GetPlayer)
            .WithName("GetPlayerAdmin")
            .WithSummary("Get player by ID")
            .WithTags("Player Management")
            .Produces<GetPlayerResponse>()
            .Produces(404);

        app.MapPatch("/players/{playerId:guid}", UpdatePlayer)
            .WithName("UpdatePlayerAdmin")
            .WithSummary("Update player")
            .WithTags("Player Management")
            .Produces<GetPlayerResponse>()
            .Produces(404)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapPost("/players/{playerId:guid}/wallet/adjust", AdjustPlayerWallet)
            .WithName("AdjustPlayerWallet")
            .WithSummary("Adjust player wallet balance")
            .WithTags("Player Management")
            .Produces<WalletAdjustmentResponse>()
            .Produces(404)
            .Produces(409)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> CreatePlayer(
        [FromBody] CreatePlayerRequest request,
        IPlayerService playerService,
        IValidator<CreatePlayerRequest> validator,
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

            // Solo SUPER_ADMIN y OPERATOR_ADMIN pueden crear jugadores
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot create players",
                    statusCode: 403);
            }

            var response = await playerService.CreatePlayerAsync(request, currentUserId);
            
            logger.LogInformation("Player created: {PlayerId} - {Username} in brand {BrandCode} by user {UserId}",
                response.Id, response.Username, response.BrandCode, currentUserId);
            
            return TypedResults.Created($"/api/v1/admin/players/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Player creation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Player Creation Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating player with username: {Username}", request.Username);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating player",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetPlayers(
        [AsParameters] QueryPlayersRequest request,
        IPlayerService playerService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);
            
            // Si estamos en un contexto de brand específico, filtramos por ese brand
            var brandScope = brandContext.IsResolved ? brandContext.BrandId : (Guid?)null;

            var response = await playerService.GetPlayersAsync(request, operatorScope, brandScope);
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting players");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting players",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetPlayer(
        Guid playerId,
        IPlayerService playerService,
        BrandContext brandContext,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);
            var brandScope = brandContext.IsResolved ? brandContext.BrandId : (Guid?)null;

            var response = await playerService.GetPlayerAsync(playerId, operatorScope, brandScope);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "Player Not Found",
                    detail: "Player does not exist or access denied",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting player: {PlayerId}", playerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting player",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdatePlayer(
        Guid playerId,
        [FromBody] UpdatePlayerRequest request,
        IPlayerService playerService,
        IValidator<UpdatePlayerRequest> validator,
        BrandContext brandContext,
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
            var brandScope = brandContext.IsResolved ? brandContext.BrandId : (Guid?)null;

            var response = await playerService.UpdatePlayerAsync(playerId, request, currentUserId, operatorScope, brandScope);
            
            logger.LogInformation("Player updated: {PlayerId} by user {UserId}",
                playerId, currentUserId);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Player update failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Player Update Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating player: {PlayerId}", playerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating player",
                statusCode: 500);
        }
    }

    private static async Task<IResult> AdjustPlayerWallet(
        Guid playerId,
        [FromBody] AdjustPlayerWalletRequest request,
        IPlayerService playerService,
        IValidator<AdjustPlayerWalletRequest> validator,
        BrandContext brandContext,
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
            var brandScope = brandContext.IsResolved ? brandContext.BrandId : (Guid?)null;

            // TODO: Validar si el usuario (especialmente CASHIER) tiene permisos para este jugador específico
            // Esto requeriría implementar la relación cashier_player

            var response = await playerService.AdjustPlayerWalletAsync(playerId, request, currentUserId, operatorScope, brandScope);
            
            if (!response.Success)
            {
                if (response.ErrorMessage?.Contains("not found") == true || response.ErrorMessage?.Contains("access denied") == true)
                {
                    return Results.Problem(
                        title: "Player Not Found",
                        detail: response.ErrorMessage,
                        statusCode: 404);
                }

                if (response.ErrorMessage?.Contains("Insufficient balance") == true)
                {
                    return Results.Problem(
                        title: "Insufficient Balance",
                        detail: response.ErrorMessage,
                        statusCode: 409);
                }

                return Results.Problem(
                    title: "Wallet Adjustment Failed",
                    detail: response.ErrorMessage,
                    statusCode: 400);
            }

            logger.LogInformation("Player wallet adjusted: {PlayerId} - Amount: {Amount} by user {UserId}",
                playerId, request.Amount, currentUserId);
            
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adjusting player wallet: {PlayerId}", playerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while adjusting player wallet",
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
            return null; // SUPER_ADMIN ve todos los jugadores

        var operatorIdClaim = httpContext.User.FindFirst("operator_id")?.Value;
        if (Guid.TryParse(operatorIdClaim, out var operatorId))
            return operatorId;

        return null;
    }
}