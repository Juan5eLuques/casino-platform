using Casino.Application.DTOs.Session;
using Casino.Domain.Entities;

namespace Casino.Application.Mappers;

public static class SessionMappers
{
    public static CreateSessionResponse ToDto(this GameSession session)
    {
        return new CreateSessionResponse(
            session.Id,
            session.PlayerId,
            session.GameCode,
            session.Provider,
            session.Status,
            session.ExpiresAt,
            session.CreatedAt);
    }

    public static GetSessionResponse ToGetDto(this GameSession session)
    {
        return new GetSessionResponse(
            session.Id,
            session.PlayerId,
            session.GameCode,
            session.Provider,
            session.Status,
            session.ExpiresAt,
            session.CreatedAt);
    }

    public static CreateRoundResponse ToDto(this Round round)
    {
        return new CreateRoundResponse(
            round.Id,
            round.SessionId,
            round.Status,
            round.CreatedAt);
    }

    public static GetRoundResponse ToGetDto(this Round round)
    {
        return new GetRoundResponse(
            round.Id,
            round.SessionId,
            round.Status,
            round.TotalBetBigint,
            round.TotalWinBigint,
            round.CreatedAt,
            round.ClosedAt);
    }

    public static CloseRoundResponse ToCloseDto(this Round round)
    {
        return new CloseRoundResponse(
            round.Id,
            round.Status,
            round.TotalBetBigint,
            round.TotalWinBigint,
            round.ClosedAt);
    }
}