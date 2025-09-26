using Casino.Application.DTOs.Session;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var sessionGroup = app.MapGroup("/api/v1/internal/sessions")
            .WithTags("Internal Sessions");

        var roundGroup = app.MapGroup("/api/v1/internal/rounds")
            .WithTags("Internal Rounds");

        // Session endpoints
        sessionGroup.MapPost("/", CreateSession)
            .WithName("InternalCreateSession")
            .WithSummary("Create a new game session")
            .Produces<CreateSessionResponse>()
            .ProducesValidationProblem();

        sessionGroup.MapGet("/{sessionId:guid}", GetSession)
            .WithName("InternalGetSession")
            .WithSummary("Get session by ID")
            .Produces<GetSessionResponse>()
            .Produces(404);

        sessionGroup.MapPost("/{sessionId:guid}/close", CloseSession)
            .WithName("InternalCloseSession")
            .WithSummary("Close a session")
            .Produces(200)
            .Produces(404);

        sessionGroup.MapPost("/{sessionId:guid}/expire", ExpireSession)
            .WithName("InternalExpireSession")
            .WithSummary("Expire a session")
            .Produces(200)
            .Produces(404);

        sessionGroup.MapGet("/player/{playerId:guid}/active", GetActiveSessions)
            .WithName("InternalGetActiveSessions")
            .WithSummary("Get active sessions for a player")
            .Produces<IEnumerable<GetSessionResponse>>();

        // Round endpoints
        roundGroup.MapPost("/", CreateRound)
            .WithName("InternalCreateRound")
            .WithSummary("Create a new round")
            .Produces<CreateRoundResponse>()
            .ProducesValidationProblem();

        roundGroup.MapGet("/{roundId:guid}", GetRound)
            .WithName("InternalGetRound")
            .WithSummary("Get round by ID")
            .Produces<GetRoundResponse>()
            .Produces(404);

        roundGroup.MapPost("/{roundId:guid}/close", CloseRound)
            .WithName("InternalCloseRound")
            .WithSummary("Close a round")
            .Produces<CloseRoundResponse>()
            .Produces(404);

        roundGroup.MapGet("/session/{sessionId:guid}", GetSessionRounds)
            .WithName("InternalGetSessionRounds")
            .WithSummary("Get all rounds for a session")
            .Produces<IEnumerable<GetRoundResponse>>();
    }

    private static async Task<IResult> CreateSession(
        [FromBody] CreateSessionRequest request,
        ISessionService sessionService,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Creating session for player: {PlayerId}, game: {GameCode}", 
                request.PlayerId, request.GameCode);
            
            var response = await sessionService.CreateSessionAsync(request);
            return TypedResults.Created($"/api/v1/internal/sessions/{response.SessionId}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Session creation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Session Creation Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating session for player: {PlayerId}", request.PlayerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating session",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetSession(
        Guid sessionId,
        ISessionService sessionService,
        ILogger<Program> logger)
    {
        try
        {
            var response = await sessionService.GetSessionAsync(sessionId);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "Session Not Found",
                    detail: "Session does not exist",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting session: {SessionId}", sessionId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting session",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CloseSession(
        Guid sessionId,
        ISessionService sessionService,
        ILogger<Program> logger)
    {
        try
        {
            var result = await sessionService.CloseSessionAsync(sessionId);
            
            if (!result)
            {
                return Results.Problem(
                    title: "Session Not Found",
                    detail: "Session does not exist",
                    statusCode: 404);
            }

            return TypedResults.Ok(new { Success = true, Message = "Session closed successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing session: {SessionId}", sessionId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while closing session",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ExpireSession(
        Guid sessionId,
        ISessionService sessionService,
        ILogger<Program> logger)
    {
        try
        {
            var result = await sessionService.ExpireSessionAsync(sessionId);
            
            if (!result)
            {
                return Results.Problem(
                    title: "Session Not Found or Not Open",
                    detail: "Session does not exist or is not in open state",
                    statusCode: 404);
            }

            return TypedResults.Ok(new { Success = true, Message = "Session expired successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error expiring session: {SessionId}", sessionId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while expiring session",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetActiveSessions(
        Guid playerId,
        ISessionService sessionService,
        ILogger<Program> logger)
    {
        try
        {
            var sessions = await sessionService.GetActiveSessionsAsync(playerId);
            return TypedResults.Ok(sessions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active sessions for player: {PlayerId}", playerId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting active sessions",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CreateRound(
        [FromBody] CreateRoundRequest request,
        IRoundService roundService,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Creating round for session: {SessionId}", request.SessionId);
            
            var response = await roundService.CreateRoundAsync(request);
            return TypedResults.Created($"/api/v1/internal/rounds/{response.RoundId}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Round creation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Round Creation Failed",
                detail: ex.Message,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating round for session: {SessionId}", request.SessionId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating round",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetRound(
        Guid roundId,
        IRoundService roundService,
        ILogger<Program> logger)
    {
        try
        {
            var response = await roundService.GetRoundAsync(roundId);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "Round Not Found",
                    detail: "Round does not exist",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting round: {RoundId}", roundId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting round",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CloseRound(
        Guid roundId,
        IRoundService roundService,
        ILogger<Program> logger)
    {
        try
        {
            var request = new CloseRoundRequest(roundId);
            var response = await roundService.CloseRoundAsync(request);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Round close failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Round Not Found",
                detail: ex.Message,
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing round: {RoundId}", roundId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while closing round",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetSessionRounds(
        Guid sessionId,
        IRoundService roundService,
        ILogger<Program> logger)
    {
        try
        {
            var rounds = await roundService.GetSessionRoundsAsync(sessionId);
            return TypedResults.Ok(rounds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting rounds for session: {SessionId}", sessionId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting session rounds",
                statusCode: 500);
        }
    }
}