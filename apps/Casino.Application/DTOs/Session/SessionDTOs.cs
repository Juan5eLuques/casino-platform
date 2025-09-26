using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Session;

public record CreateSessionRequest(
    Guid PlayerId,
    string GameCode,
    string Provider,
    int ExpirationMinutes = 60);

public record CreateSessionResponse(
    Guid SessionId,
    Guid PlayerId,
    string GameCode,
    string Provider,
    GameSessionStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public record GetSessionResponse(
    Guid SessionId,
    Guid PlayerId,
    string GameCode,
    string Provider,
    GameSessionStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public record CreateRoundRequest(
    Guid SessionId,
    Guid? RoundId = null);

public record CreateRoundResponse(
    Guid RoundId,
    Guid SessionId,
    RoundStatus Status,
    DateTime CreatedAt);

public record GetRoundResponse(
    Guid RoundId,
    Guid SessionId,
    RoundStatus Status,
    long TotalBetBigint,
    long TotalWinBigint,
    DateTime CreatedAt,
    DateTime? ClosedAt);

public record CloseRoundRequest(
    Guid RoundId);

public record CloseRoundResponse(
    Guid RoundId,
    RoundStatus Status,
    long TotalBetBigint,
    long TotalWinBigint,
    DateTime? ClosedAt);