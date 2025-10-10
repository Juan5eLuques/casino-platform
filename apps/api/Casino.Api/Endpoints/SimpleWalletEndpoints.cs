using Casino.Api.Utils;
using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

/// <summary>
/// SONNET: Endpoints de transacciones SIMPLE+ sin conflictos de compilación
/// Un solo MapGroup, nombres únicos, autorización granular
/// </summary>
public static class SimpleWalletEndpoints
{
    public static void MapSimpleWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Wallet Transactions")
            .RequireAuthorization("AnyBackofficeUser");

        // SONNET: POST /transactions - Crear transacción (MINT o TRANSFER)
        group.MapPost("/transactions", CreateTransaction)
            .RequireAuthorization() // Autorización específica dentro del endpoint
            .WithName("CreateWalletTransaction")
            .WithSummary("Create wallet transaction (MINT or TRANSFER)")
            .WithDescription("MINT: fromUserId=null (SUPER_ADMIN only). TRANSFER: both users specified. Idempotent via IdempotencyKey.")
            .Produces<TransactionResponse>(201)
            .Produces(400) // validation_error, brand_not_resolved
            .Produces(403) // forbidden_scope, forbidden_role
            .Produces(404) // user_not_found, cross_brand_forbidden
            .Produces(409); // insufficient_funds, idempotency_conflict

        // SONNET: GET /transactions - Lista de transacciones con scope
        group.MapGet("/transactions", ListTransactions)
            .RequireAuthorization("AnyBackofficeUser") // SONNET: Cambiado de BrandScopedCashierOrAdmin para permitir 
            .WithName("ListWalletTransactions")
            .WithSummary("List wallet transactions with filters")
            .WithDescription("Lists transactions respecting role scope. SUPER_ADMIN can use ?globalScope=true.")
            .Produces<GetTransactionsResponse>()
            .Produces(400)
            .Produces(403);

        // SONNET: GET /users/{id}/balance - Balance de usuario
        group.MapGet("/users/{userId:guid}/balance", GetUserBalance)
            .RequireAuthorization("AnyBackofficeUser") // SONNET: Cambiado de BrandScopedCashierOrAdmin para permitir SUPER_ADMIN
            .WithName("GetUserWalletBalance")
            .WithSummary("Get user wallet balance")
            .WithDescription("Gets current wallet balance. Specify userType query parameter.")
            .Produces<SimpleWalletBalanceResponse>()
            .Produces(404)
            .Produces(403);
    }

    /// <summary>
    /// SONNET: Crear transacción con FluentValidation y autorización granular por tipo de operación
    /// </summary>
    private static async Task<IResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        [FromServices] ISimpleWalletService walletService,
        [FromServices] IValidator<CreateTransactionRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("SimpleWalletEndpoints");

        // SONNET: Validación con FluentValidation
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // SONNET: Determinar tipo de operación para autorización específica
            bool isMint = !request.FromUserId.HasValue;

            // SONNET: Validar autorización específica por tipo de operación
            if (isMint && currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied", 
                    detail: "Only SUPER_ADMIN can create money (MINT)", 
                    statusCode: 403);
            }

            // SONNET: Para operaciones no-MINT, validar brand context
            if (!isMint || currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            // SONNET: Validar que el brand esté resuelto
            if (!brandContext.IsResolved)
            {
                return Results.BadRequest(new { error = "brand_not_resolved", message = "Brand context could not be resolved from host" });
            }

            var brandId = brandContext.BrandId;

            var response = await walletService.CreateTransactionAsync(request, currentUserId, currentRole, brandId);

            logger.LogInformation("Transaction created: {TransactionId} - {Type} - Amount: {Amount} - Actor: {UserId}",
                response.Id, response.Type, response.Amount, currentUserId);

            return Results.Created($"/api/v1/admin/transactions/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Transaction creation failed: {Error}", ex.Message);

            // SONNET: Mapear errores específicos a códigos HTTP apropiados
            if (ex.Message.Contains("Insufficient"))
                return Results.Conflict(new { error = "insufficient_funds", message = ex.Message });

            if (ex.Message.Contains("not found"))
                return Results.NotFound(new { error = "user_not_found", message = ex.Message });

            if (ex.Message.Contains("brand") || ex.Message.Contains("cross"))
                return Results.NotFound(new { error = "cross_brand_forbidden", message = ex.Message });

            if (ex.Message.Contains("Only") || ex.Message.Contains("privileges"))
                return Results.Problem(title: "Access Denied", detail: ex.Message, statusCode: 403);

            return Results.BadRequest(new { error = "invalid_payload", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating transaction");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    /// <summary>
    /// SONNET: Listar transacciones con scope por rol
    /// </summary>
    private static async Task<IResult> ListTransactions(
        [AsParameters] GetTransactionsRequest request,
        [FromServices] ISimpleWalletService walletService,
        [FromServices] IValidator<GetTransactionsRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("SimpleWalletEndpoints");

        // SONNET: Validación con FluentValidation
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // SONNET: Validar brand context (excepto SUPER_ADMIN con GlobalScope)
            if (!request.GlobalScope || currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            // SONNET: Solo SUPER_ADMIN puede usar GlobalScope
            if (request.GlobalScope && currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied", 
                    detail: "Only SUPER_ADMIN can access global scope", 
                    statusCode: 403);
            }

            var queryScope = request.GlobalScope ? null : AuthorizationHelper.GetQueryScope(
                currentRole, currentUserBrandId, brandContext);

            var response = await walletService.GetTransactionsAsync(request, queryScope, currentUserId, currentRole);

            logger.LogInformation("Listed {Count} transactions for role {Role} (globalScope: {GlobalScope})", 
                response.TotalCount, currentRole, request.GlobalScope);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing transactions");
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }

    /// <summary>
    /// SONNET: Obtener balance de usuario con scope
    /// </summary>
    private static async Task<IResult> GetUserBalance(
        Guid userId,
        [FromQuery] string userType,
        [FromServices] ISimpleWalletService walletService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("SimpleWalletEndpoints");

        // SONNET: Validar userType
        if (userType != "BACKOFFICE" && userType != "PLAYER")
        {
            return Results.BadRequest(new { error = "invalid_user_type", message = "userType must be BACKOFFICE or PLAYER" });
        }

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // SONNET: Validar brand context para roles no-SUPER_ADMIN
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            var balance = await walletService.GetBalanceAsync(userId, userType);

            if (balance == null)
            {
                return Results.NotFound(new { error = "user_not_found", userId, userType });
            }

            return Results.Ok(balance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting balance for user {UserId} type {UserType}", userId, userType);
            return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
    }
}