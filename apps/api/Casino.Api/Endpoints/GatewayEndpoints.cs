using Casino.Application.DTOs.Gateway;
using Casino.Application.DTOs.Wallet;
using Casino.Application.Services;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casino.Api.Endpoints;

public static class GatewayEndpoints
{
    public static void MapGatewayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/gateway")
            .WithTags("Gateway")
            .AddEndpointFilter<Casino.Api.Filters.HmacEndpointFilter>();

        group.MapPost("/balance", GetBalance)
            .WithName("GatewayGetBalance")
            .WithSummary("Get player balance for game provider")
            .Produces<BalanceGatewayResponse>()
            .Produces(401)
            .Produces(404);

        group.MapPost("/bet", PlaceBet)
            .WithName("GatewayPlaceBet")
            .WithSummary("Place a bet")
            .Produces<GatewayResponse>()
            .Produces(401)
            .Produces(409);

        group.MapPost("/win", ProcessWin)
            .WithName("GatewayProcessWin")
            .WithSummary("Process a win")
            .Produces<GatewayResponse>()
            .Produces(401);

        group.MapPost("/rollback", ProcessRollback)
            .WithName("GatewayProcessRollback")
            .WithSummary("Rollback a transaction")
            .Produces<GatewayResponse>()
            .Produces(401)
            .Produces(404);

        group.MapPost("/closeRound", CloseRound)
            .WithName("GatewayCloseRound")
            .WithSummary("Close a game round")
            .Produces<GatewayResponse>()
            .Produces(401);
    }

    private static async Task<IResult> GetBalance(
        [FromBody] BalanceGatewayRequest request,
        IWalletService walletService,
        IAuditService auditService,
        CasinoDbContext context,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var provider = httpContext.Items["Provider"]?.ToString() ?? "unknown";
        
        try
        {
            // Validate session and get player
            var session = await context.GameSessions
                .Include(s => s.Player)
                .FirstOrDefaultAsync(s => s.Id.ToString() == request.SessionId && 
                                         s.Status == GameSessionStatus.OPEN);

            if (session == null)
            {
                logger.LogWarning("Session not found or expired: {SessionId}", request.SessionId);
                await auditService.LogProviderActionAsync(provider, "GET_BALANCE", request.SessionId, 
                    request.PlayerId, requestData: request, statusCode: 404);
                
                return Results.Problem(
                    title: "Session Not Found",
                    detail: "Invalid or expired session",
                    statusCode: 404);
            }

            if (session.PlayerId.ToString() != request.PlayerId)
            {
                logger.LogWarning("Player ID mismatch in session: {SessionId}", request.SessionId);
                await auditService.LogProviderActionAsync(provider, "GET_BALANCE", request.SessionId, 
                    request.PlayerId, requestData: request, statusCode: 400);
                
                return Results.Problem(
                    title: "Player Mismatch",
                    detail: "Player ID does not match session",
                    statusCode: 400);
            }

            var balanceRequest = new BalanceRequest(session.PlayerId);
            var response = await walletService.GetBalanceAsync(balanceRequest);

            var gatewayResponse = new BalanceGatewayResponse(response.Balance);
            
            logger.LogInformation("Balance retrieved for session: {SessionId}, Provider: {Provider}, Balance: {Balance}", 
                request.SessionId, provider, response.Balance);
            
            await auditService.LogProviderActionAsync(provider, "GET_BALANCE", request.SessionId, 
                request.PlayerId, requestData: request, responseData: gatewayResponse);

            return TypedResults.Ok(gatewayResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting balance for session: {SessionId}", request.SessionId);
            await auditService.LogProviderActionAsync(provider, "GET_BALANCE", request.SessionId, 
                request.PlayerId, requestData: request, statusCode: 500);
            
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting balance",
                statusCode: 500);
        }
    }

    private static async Task<IResult> PlaceBet(
        [FromBody] BetRequest request,
        IWalletService walletService,
        IAuditService auditService,
        CasinoDbContext context,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var provider = httpContext.Items["Provider"]?.ToString() ?? "unknown";
        
        try
        {
            // Validate session
            var session = await context.GameSessions
                .Include(s => s.Player)
                .FirstOrDefaultAsync(s => s.Id.ToString() == request.SessionId && 
                                         s.Status == GameSessionStatus.OPEN);

            if (session == null)
            {
                await auditService.LogProviderActionAsync(provider, "PLACE_BET", request.SessionId, 
                    request.PlayerId, request.RoundId, request.TxId, requestData: request, statusCode: 404);
                
                return Results.Problem(
                    title: "Session Not Found",
                    detail: "Invalid or expired session",
                    statusCode: 404);
            }

            if (session.PlayerId.ToString() != request.PlayerId)
            {
                await auditService.LogProviderActionAsync(provider, "PLACE_BET", request.SessionId, 
                    request.PlayerId, request.RoundId, request.TxId, requestData: request, statusCode: 400);
                
                return Results.Problem(
                    title: "Player Mismatch",
                    detail: "Player ID does not match session",
                    statusCode: 400);
            }

            // Get or create round
            var roundId = Guid.Parse(request.RoundId);
            var round = await context.Rounds.FirstOrDefaultAsync(r => r.Id == roundId);
            if (round == null)
            {
                round = new Casino.Domain.Entities.Round
                {
                    Id = roundId,
                    SessionId = session.Id,
                    Status = RoundStatus.OPEN,
                    TotalBetBigint = 0,
                    TotalWinBigint = 0,
                    CreatedAt = DateTime.UtcNow
                };
                context.Rounds.Add(round);
                await context.SaveChangesAsync();
            }

            // Process bet
            var debitRequest = new DebitRequest(
                session.PlayerId,
                request.Amount,
                LedgerReason.BET,
                roundId,
                request.TxId,
                session.GameCode,
                provider);

            var response = await walletService.DebitAsync(debitRequest);

            var gatewayResponse = new GatewayResponse(response.Success, response.Balance, response.ErrorMessage);

            if (!response.Success)
            {
                logger.LogWarning("Bet failed for session: {SessionId}, Amount: {Amount}, Error: {Error}", 
                    request.SessionId, request.Amount, response.ErrorMessage);
                
                await auditService.LogProviderActionAsync(provider, "PLACE_BET", request.SessionId, 
                    request.PlayerId, request.RoundId, request.TxId, requestData: request, responseData: gatewayResponse, 
                    statusCode: response.ErrorMessage?.Contains("Insufficient balance") == true ? 409 : 400);
                
                if (response.ErrorMessage?.Contains("Insufficient balance") == true)
                {
                    return TypedResults.Ok(gatewayResponse);
                }
                
                return Results.Problem(
                    title: "Bet Failed",
                    detail: response.ErrorMessage,
                    statusCode: 400);
            }

            // Update round totals
            round.TotalBetBigint += request.Amount;
            await context.SaveChangesAsync();

            logger.LogInformation("Bet placed successfully for session: {SessionId}, Amount: {Amount}, TxId: {TxId}", 
                request.SessionId, request.Amount, request.TxId);

            await auditService.LogProviderActionAsync(provider, "PLACE_BET", request.SessionId, 
                request.PlayerId, request.RoundId, request.TxId, requestData: request, responseData: gatewayResponse);

            return TypedResults.Ok(gatewayResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error placing bet for session: {SessionId}", request.SessionId);
            await auditService.LogProviderActionAsync(provider, "PLACE_BET", request.SessionId, 
                request.PlayerId, request.RoundId, request.TxId, requestData: request, statusCode: 500);
            
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while placing bet",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ProcessWin(
        [FromBody] WinRequest request,
        IWalletService walletService,
        IAuditService auditService,
        CasinoDbContext context,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var provider = httpContext.Items["Provider"]?.ToString() ?? "unknown";
        
        try
        {
            // Validate session
            var session = await context.GameSessions
                .Include(s => s.Player)
                .FirstOrDefaultAsync(s => s.Id.ToString() == request.SessionId && 
                                         s.Status == GameSessionStatus.OPEN);

            if (session == null)
            {
                await auditService.LogProviderActionAsync(provider, "PROCESS_WIN", request.SessionId, 
                    request.PlayerId, request.RoundId, request.TxId, requestData: request, statusCode: 404);
                
                return Results.Problem(
                    title: "Session Not Found",
                    detail: "Invalid or expired session",
                    statusCode: 404);
            }

            if (session.PlayerId.ToString() != request.PlayerId)
            {
                await auditService.LogProviderActionAsync(provider, "PROCESS_WIN", request.SessionId, 
                    request.PlayerId, request.RoundId, request.TxId, requestData: request, statusCode: 400);
                
                return Results.Problem(
                    title: "Player Mismatch",
                    detail: "Player ID does not match session",
                    statusCode: 400);
            }

            // Get round
            var roundId = Guid.Parse(request.RoundId);
            var round = await context.Rounds.FirstOrDefaultAsync(r => r.Id == roundId);
            if (round == null)
            {
                await auditService.LogProviderActionAsync(provider, "PROCESS_WIN", request.SessionId, 
                    request.PlayerId, request.RoundId, request.TxId, requestData: request, statusCode: 404);
                
                return Results.Problem(
                    title: "Round Not Found",
                    detail: "Round does not exist",
                    statusCode: 404);
            }

            // Process win
            var creditRequest = new CreditRequest(
                session.PlayerId,
                request.Amount,
                LedgerReason.WIN,
                roundId,
                request.TxId,
                session.GameCode,
                provider);

            var response = await walletService.CreditAsync(creditRequest);

            var gatewayResponse = new GatewayResponse(response.Success, response.Balance, response.ErrorMessage);

            if (!response.Success)
            {
                logger.LogWarning("Win processing failed for session: {SessionId}, Amount: {Amount}, Error: {Error}", 
                    request.SessionId, request.Amount, response.ErrorMessage);
                
                await auditService.LogProviderActionAsync(provider, "PROCESS_WIN", request.SessionId, 
                    request.PlayerId, request.RoundId, request.TxId, requestData: request, responseData: gatewayResponse, statusCode: 400);
                
                return Results.Problem(
                    title: "Win Processing Failed",
                    detail: response.ErrorMessage,
                    statusCode: 400);
            }

            // Update round totals
            round.TotalWinBigint += request.Amount;
            await context.SaveChangesAsync();

            logger.LogInformation("Win processed successfully for session: {SessionId}, Amount: {Amount}, TxId: {TxId}", 
                request.SessionId, request.Amount, request.TxId);

            await auditService.LogProviderActionAsync(provider, "PROCESS_WIN", request.SessionId, 
                request.PlayerId, request.RoundId, request.TxId, requestData: request, responseData: gatewayResponse);

            return TypedResults.Ok(gatewayResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing win for session: {SessionId}", request.SessionId);
            await auditService.LogProviderActionAsync(provider, "PROCESS_WIN", request.SessionId, 
                request.PlayerId, request.RoundId, request.TxId, requestData: request, statusCode: 500);
            
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while processing win",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ProcessRollback(
        [FromBody] RollbackGatewayRequest request,
        IWalletService walletService,
        IAuditService auditService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var provider = httpContext.Items["Provider"]?.ToString() ?? "unknown";
        
        try
        {
            var rollbackRequest = new RollbackRequest(request.TxIdOriginal);
            var response = await walletService.RollbackAsync(rollbackRequest);

            var gatewayResponse = new GatewayResponse(response.Success, response.Balance, response.ErrorMessage);

            if (!response.Success)
            {
                logger.LogWarning("Rollback failed for TxId: {TxId}, Error: {Error}", 
                    request.TxIdOriginal, response.ErrorMessage);
                
                var statusCode = response.ErrorMessage?.Contains("not found") == true ? 404 : 400;
                await auditService.LogProviderActionAsync(provider, "PROCESS_ROLLBACK", 
                    externalRef: request.TxIdOriginal, requestData: request, responseData: gatewayResponse, statusCode: statusCode);
                
                if (response.ErrorMessage?.Contains("not found") == true)
                {
                    return Results.Problem(
                        title: "Transaction Not Found",
                        detail: response.ErrorMessage,
                        statusCode: 404);
                }
                
                return Results.Problem(
                    title: "Rollback Failed",
                    detail: response.ErrorMessage,
                    statusCode: 400);
            }

            logger.LogInformation("Rollback processed successfully for TxId: {TxId}", request.TxIdOriginal);

            await auditService.LogProviderActionAsync(provider, "PROCESS_ROLLBACK", 
                externalRef: request.TxIdOriginal, requestData: request, responseData: gatewayResponse);

            return TypedResults.Ok(gatewayResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing rollback for TxId: {TxId}", request.TxIdOriginal);
            await auditService.LogProviderActionAsync(provider, "PROCESS_ROLLBACK", 
                externalRef: request.TxIdOriginal, requestData: request, statusCode: 500);
            
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while processing rollback",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CloseRound(
        [FromBody] CloseRoundRequest request,
        IAuditService auditService,
        CasinoDbContext context,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var provider = httpContext.Items["Provider"]?.ToString() ?? "unknown";
        
        try
        {
            // Validate session
            var session = await context.GameSessions
                .FirstOrDefaultAsync(s => s.Id.ToString() == request.SessionId && 
                                         s.Status == GameSessionStatus.OPEN);

            if (session == null)
            {
                await auditService.LogProviderActionAsync(provider, "CLOSE_ROUND", request.SessionId, 
                    roundId: request.RoundId, requestData: request, statusCode: 404);
                
                return Results.Problem(
                    title: "Session Not Found",
                    detail: "Invalid or expired session",
                    statusCode: 404);
            }

            // Get and close round
            var roundId = Guid.Parse(request.RoundId);
            var round = await context.Rounds.FirstOrDefaultAsync(r => r.Id == roundId);
            if (round == null)
            {
                await auditService.LogProviderActionAsync(provider, "CLOSE_ROUND", request.SessionId, 
                    roundId: request.RoundId, requestData: request, statusCode: 404);
                
                return Results.Problem(
                    title: "Round Not Found",
                    detail: "Round does not exist",
                    statusCode: 404);
            }

            round.Status = RoundStatus.CLOSED;
            round.ClosedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            logger.LogInformation("Round closed successfully: {RoundId}, Session: {SessionId}", 
                request.RoundId, request.SessionId);

            var gatewayResponse = new GatewayResponse(true, 0);
            await auditService.LogProviderActionAsync(provider, "CLOSE_ROUND", request.SessionId, 
                roundId: request.RoundId, requestData: request, responseData: gatewayResponse);

            return TypedResults.Ok(gatewayResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing round: {RoundId}", request.RoundId);
            await auditService.LogProviderActionAsync(provider, "CLOSE_ROUND", request.SessionId, 
                roundId: request.RoundId, requestData: request, statusCode: 500);
            
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while closing round",
                statusCode: 500);
        }
    }
}
