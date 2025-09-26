using Casino.Application.DTOs.Session;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class SessionService : ISessionService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<SessionService> _logger;

    public SessionService(CasinoDbContext context, ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request)
    {
        try
        {
            // Verify player exists and is active
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId && p.Status == PlayerStatus.ACTIVE);

            if (player == null)
            {
                throw new InvalidOperationException("Player not found or inactive");
            }

            // Close any existing open sessions for this player/game combination
            var existingSessions = await _context.GameSessions
                .Where(s => s.PlayerId == request.PlayerId && 
                           s.GameCode == request.GameCode && 
                           s.Status == GameSessionStatus.OPEN)
                .ToListAsync();

            foreach (var session in existingSessions)
            {
                session.Status = GameSessionStatus.CLOSED;
                _logger.LogInformation("Closed existing session {SessionId} for player {PlayerId}", 
                    session.Id, request.PlayerId);
            }

            // Create new session
            var newSession = new GameSession
            {
                Id = Guid.NewGuid(),
                PlayerId = request.PlayerId,
                GameCode = request.GameCode,
                Provider = request.Provider,
                Status = GameSessionStatus.OPEN,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.ExpirationMinutes),
                CreatedAt = DateTime.UtcNow
            };

            _context.GameSessions.Add(newSession);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new session {SessionId} for player {PlayerId} and game {GameCode}", 
                newSession.Id, request.PlayerId, request.GameCode);

            return new CreateSessionResponse(
                newSession.Id,
                newSession.PlayerId,
                newSession.GameCode,
                newSession.Provider,
                newSession.Status,
                newSession.ExpiresAt,
                newSession.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for player {PlayerId}", request.PlayerId);
            throw;
        }
    }

    public async Task<GetSessionResponse?> GetSessionAsync(Guid sessionId)
    {
        var session = await _context.GameSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return null;

        return new GetSessionResponse(
            session.Id,
            session.PlayerId,
            session.GameCode,
            session.Provider,
            session.Status,
            session.ExpiresAt,
            session.CreatedAt);
    }

    public async Task<bool> ExpireSessionAsync(Guid sessionId)
    {
        var session = await _context.GameSessions.FindAsync(sessionId);
        if (session == null || session.Status != GameSessionStatus.OPEN)
            return false;

        session.Status = GameSessionStatus.EXPIRED;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Expired session {SessionId}", sessionId);
        return true;
    }

    public async Task<bool> CloseSessionAsync(Guid sessionId)
    {
        var session = await _context.GameSessions.FindAsync(sessionId);
        if (session == null)
            return false;

        session.Status = GameSessionStatus.CLOSED;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Closed session {SessionId}", sessionId);
        return true;
    }

    public async Task<IEnumerable<GetSessionResponse>> GetActiveSessionsAsync(Guid playerId)
    {
        var sessions = await _context.GameSessions
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId && s.Status == GameSessionStatus.OPEN)
            .ToListAsync();

        return sessions.Select(s => new GetSessionResponse(
            s.Id,
            s.PlayerId,
            s.GameCode,
            s.Provider,
            s.Status,
            s.ExpiresAt,
            s.CreatedAt));
    }
}