using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Application.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/internal/wallet")
            .WithTags("Internal Wallet");

        group.MapPost("/balance", GetBalance)
            .WithName("InternalGetBalance")
            .WithSummary("Get player balance")
            .Produces<BalanceResponse>()
            .ProducesValidationProblem();

        group.MapPost("/debit", DebitWallet)
            .WithName("InternalDebitWallet")
            .WithSummary("Debit amount from player wallet")
            .Produces<WalletOperationResponse>()
            .Produces(409)
            .ProducesValidationProblem();

        group.MapPost("/credit", CreditWallet)
            .WithName("InternalCreditWallet")
            .WithSummary("Credit amount to player wallet")
            .Produces<WalletOperationResponse>()
            .ProducesValidationProblem();

        group.MapPost("/rollback", RollbackTransaction)
            .WithName("InternalRollbackTransaction")
            .WithSummary("Rollback a previous transaction")
            .Produces<WalletOperationResponse>()
            .Produces(409)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetBalance(
        [FromBody] BalanceRequest request,
        IWalletService walletService,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Getting balance for player: {PlayerId}", request.PlayerId);
            var response = await walletService.GetBalanceAsync(request);
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting balance for player: {PlayerId}", request.PlayerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting balance",
                statusCode: 500);
        }
    }

    private static async Task<IResult> DebitWallet(
        [FromBody] DebitRequest request,
        IWalletService walletService,
        IValidator<DebitRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            logger.LogInformation("Debiting wallet for player: {PlayerId}, Amount: {Amount}, ExternalRef: {ExternalRef}", 
                request.PlayerId, request.Amount, request.ExternalRef);
            
            var response = await walletService.DebitAsync(request);
            
            if (!response.Success)
            {
                if (response.ErrorMessage?.Contains("Insufficient balance") == true)
                {
                    return Results.Problem(
                        title: "Insufficient Balance",
                        detail: response.ErrorMessage,
                        statusCode: 409);
                }
                
                return Results.Problem(
                    title: "Debit Failed",
                    detail: response.ErrorMessage,
                    statusCode: 400);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error debiting wallet for player: {PlayerId}", request.PlayerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while debiting wallet",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CreditWallet(
        [FromBody] CreditRequest request,
        IWalletService walletService,
        IValidator<CreditRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            logger.LogInformation("Crediting wallet for player: {PlayerId}, Amount: {Amount}, ExternalRef: {ExternalRef}", 
                request.PlayerId, request.Amount, request.ExternalRef);
            
            var response = await walletService.CreditAsync(request);
            
            if (!response.Success)
            {
                return Results.Problem(
                    title: "Credit Failed",
                    detail: response.ErrorMessage,
                    statusCode: 400);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error crediting wallet for player: {PlayerId}", request.PlayerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while crediting wallet",
                statusCode: 500);
        }
    }

    private static async Task<IResult> RollbackTransaction(
        [FromBody] RollbackRequest request,
        IWalletService walletService,
        IValidator<RollbackRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            logger.LogInformation("Rolling back transaction: {ExternalRef}", request.ExternalRefOriginal);
            
            var response = await walletService.RollbackAsync(request);
            
            if (!response.Success)
            {
                if (response.ErrorMessage?.Contains("negative balance") == true)
                {
                    return Results.Problem(
                        title: "Rollback Failed",
                        detail: response.ErrorMessage,
                        statusCode: 409);
                }
                
                return Results.Problem(
                    title: "Rollback Failed",
                    detail: response.ErrorMessage,
                    statusCode: 400);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rolling back transaction: {ExternalRef}", request.ExternalRefOriginal);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while rolling back transaction",
                statusCode: 500);
        }
    }
}