using Casino.Application.DTOs.Session;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class RoundService : IRoundService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<RoundService> _logger;

    public RoundService(CasinoDbContext context, ILogger<RoundService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateRoundResponse> CreateRoundAsync(CreateRoundRequest request)
    {
        try
        {
            // Verify session exists and is open
            var session = await _context.GameSessions
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.Status == GameSessionStatus.OPEN);

            if (session == null)
            {
                throw new InvalidOperationException("Session not found or not open");
            }

            var roundId = request.RoundId ?? Guid.NewGuid();

            // Check if round already exists
            var existingRound = await _context.Rounds.FindAsync(roundId);
            if (existingRound != null)
            {
                return new CreateRoundResponse(
                    existingRound.Id,
                    existingRound.SessionId,
                    existingRound.Status,
                    existingRound.CreatedAt);
            }

            // Create new round
            var newRound = new Round
            {
                Id = roundId,
                SessionId = request.SessionId,
                Status = RoundStatus.OPEN,
                TotalBetBigint = 0,
                TotalWinBigint = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Rounds.Add(newRound);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new round {RoundId} for session {SessionId}", 
                newRound.Id, request.SessionId);

            return new CreateRoundResponse(
                newRound.Id,
                newRound.SessionId,
                newRound.Status,
                newRound.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating round for session {SessionId}", request.SessionId);
            throw;
        }
    }

    public async Task<GetRoundResponse?> GetRoundAsync(Guid roundId)
    {
        var round = await _context.Rounds
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roundId);

        if (round == null)
            return null;

        return new GetRoundResponse(
            round.Id,
            round.SessionId,
            round.Status,
            round.TotalBetBigint,
            round.TotalWinBigint,
            round.CreatedAt,
            round.ClosedAt);
    }

    public async Task<CloseRoundResponse> CloseRoundAsync(CloseRoundRequest request)
    {
        var round = await _context.Rounds.FindAsync(request.RoundId);
        if (round == null)
        {
            throw new InvalidOperationException("Round not found");
        }

        if (round.Status == RoundStatus.CLOSED)
        {
            // Already closed, return current state
            return new CloseRoundResponse(
                round.Id,
                round.Status,
                round.TotalBetBigint,
                round.TotalWinBigint,
                round.ClosedAt);
        }

        round.Status = RoundStatus.CLOSED;
        round.ClosedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Closed round {RoundId}", request.RoundId);

        return new CloseRoundResponse(
            round.Id,
            round.Status,
            round.TotalBetBigint,
            round.TotalWinBigint,
            round.ClosedAt);
    }

    public async Task<IEnumerable<GetRoundResponse>> GetSessionRoundsAsync(Guid sessionId)
    {
        var rounds = await _context.Rounds
            .AsNoTracking()
            .Where(r => r.SessionId == sessionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rounds.Select(r => new GetRoundResponse(
            r.Id,
            r.SessionId,
            r.Status,
            r.TotalBetBigint,
            r.TotalWinBigint,
            r.CreatedAt,
            r.ClosedAt));
    }
}