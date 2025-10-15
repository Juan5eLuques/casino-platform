using Casino.Api.Utils;
using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

/// <summary>
/// Endpoints administrativos unificados que usan UnifiedWalletService
/// Maneja TODOS los tipos de transacciones con TransactionType enum
/// Reemplaza a SimpleWalletEndpoints para unificar gateway y backoffice
/// </summary>
public static class AdminTransactionEndpoints
{
    public static void MapAdminTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin Transactions")
            .RequireAuthorization("AnyBackofficeUser");

        // POST /api/v1/admin/transactions - Crear transacción con TransactionType
        group.MapPost("/transactions", CreateAdminTransaction)
            .WithName("CreateAdminTransaction")
            .WithSummary("Create transaction using UnifiedWalletService")
            .WithDescription("Creates transactions with full TransactionType support (BET, WIN, TRANSFER, MINT, DEPOSIT, etc.)")
            .Produces<AdminTransactionResponse>(201)
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .Produces(409);

        // GET /api/v1/admin/transactions - Listar transacciones con filtros
        group.MapGet("/transactions", ListAdminTransactions)
            .WithName("ListAdminTransactions")
            .WithSummary("List transactions with TransactionType filters")
            .WithDescription("Lists all WalletTransactions with full filtering by TransactionType")
            .Produces<GetAdminTransactionsResponse>()
            .Produces(400)
            .Produces(403);

        // POST /api/v1/admin/transactions/rollback - Revertir transacción
        group.MapPost("/transactions/rollback", RollbackAdminTransaction)
            .WithName("RollbackAdminTransaction")
            .WithSummary("Rollback transaction by ExternalRef")
            .WithDescription("Rollback any transaction using UnifiedWalletService")
            .Produces<AdminTransactionResponse>()
            .Produces(400)
            .Produces(404)
            .Produces(409);

        // GET /api/v1/admin/users/{userId}/balance?userType=PLAYER|BACKOFFICE - Balance de usuario
        group.MapGet("/users/{userId:guid}/balance", GetUserBalance)
            .WithName("GetUserBalance")
            .WithSummary("Get user balance (PLAYER or BACKOFFICE)")
            .WithDescription("Gets balance from Player.WalletBalance or BackofficeUser.WalletBalance based on userType query parameter (PLAYER|BACKOFFICE)")
            .Produces<object>() // Response: { userId, userType, username, balance }
            .Produces(400) // Invalid userType parameter
            .Produces(404) // User not found
            .Produces(403); // Access denied
    }

    private static async Task<IResult> CreateAdminTransaction(
        [FromBody] CreateAdminTransactionRequest request,
        [FromServices] IAdminTransactionService adminTransactionService,
        [FromServices] IValidator<CreateAdminTransactionRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        // Validación
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Determinar tipo de operación
            bool isMint = !request.FromUserId.HasValue;

            // Validar autorización según tipo de operación
            if (isMint && currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN can create MINT transactions",
                    statusCode: 403);
            }

            // Validar brand context para roles no-SUPER_ADMIN
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            if (!brandContext.IsResolved)
            {
                return Results.BadRequest(new { error = "brand_not_resolved", message = "Brand context could not be resolved" });
            }

            var response = await adminTransactionService.CreateTransactionAsync(
                request, currentUserId, currentRole, brandContext.BrandId);

            logger.LogInformation("Admin transaction created: {TransactionId} - Type: {Type} - ToUser: {ToUserId} - Amount: {Amount}",
                response.Id, response.TransactionType, response.ToUserId, response.Amount);

            return Results.Created($"/api/v1/admin/transactions/{response.Id}", response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient"))
        {
            return Results.Problem(
                title: "Insufficient Balance",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound(new { error = "resource_not_found", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("brand") || ex.Message.Contains("access"))
        {
            return Results.Problem(
                title: "Access Denied",
                detail: ex.Message,
                statusCode: 403);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating admin transaction");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating the transaction",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ListAdminTransactions(
        [AsParameters] GetAdminTransactionsRequest request,
        [FromServices] IAdminTransactionService adminTransactionService,
        [FromServices] IValidator<GetAdminTransactionsRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        // Validación
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar GlobalScope solo para SUPER_ADMIN
            if (request.GlobalScope && currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN can use global scope",
                    statusCode: 403);
            }

            // Determinar scope
            Guid? queryScope = null;
            if (!request.GlobalScope || currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                if (!brandContext.IsResolved)
                {
                    return Results.BadRequest(new { error = "brand_not_resolved", message = "Brand context required" });
                }
                queryScope = brandContext.BrandId;
            }

            var response = await adminTransactionService.GetTransactionsAsync(
                request, queryScope, currentUserId, currentRole);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing admin transactions");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while listing transactions",
                statusCode: 500);
        }
    }

    private static async Task<IResult> RollbackAdminTransaction(
        [FromBody] AdminRollbackRequest request,
        [FromServices] IAdminTransactionService adminTransactionService,
        [FromServices] IValidator<AdminRollbackRequest> validator,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        // Validación
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(httpContext);
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            var response = await adminTransactionService.RollbackTransactionAsync(
                request, currentUserId, currentRole, brandContext.BrandId);

            logger.LogInformation("Admin transaction rolled back: {TransactionId} - ExternalRef: {ExternalRef}",
                response.Id, request.ExternalRef);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound(new { error = "transaction_not_found", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already rolled back"))
        {
            return Results.Problem(
                title: "Already Processed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rolling back admin transaction");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while rolling back the transaction",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetUserBalance(
        Guid userId,
        [FromQuery] string userType,
        [FromServices] IAdminTransactionService adminTransactionService,
        [FromServices] BrandContext brandContext,
        HttpContext httpContext,
        [FromServices] ILogger<Program> logger)
    {
        // Validar userType
        if (userType != "BACKOFFICE" && userType != "PLAYER")
        {
            return Results.BadRequest(new { error = "invalid_user_type", message = "userType must be BACKOFFICE or PLAYER" });
        }

        try
        {
            var currentRole = AuthorizationHelper.GetCurrentUserRole(httpContext);
            var currentUserBrandId = AuthorizationHelper.GetCurrentUserBrandId(httpContext);

            // Validar brand context para roles no-SUPER_ADMIN
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                var brandValidation = AuthorizationHelper.ValidateBrandScopedOperation(
                    currentRole, currentUserBrandId, brandContext, requireBrandContext: true);
                if (brandValidation != null) return brandValidation;
            }

            var balanceResponse = await adminTransactionService.GetUserBalanceAsync(userId, userType);

            if (balanceResponse == null)
            {
                return Results.NotFound(new { error = "user_not_found", userId, userType });
            }

            return Results.Ok(balanceResponse);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound(new { error = "user_not_found", userId, userType });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user balance for {UserId} type {UserType}", userId, userType);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting user balance",
                statusCode: 500);
        }
    }
}