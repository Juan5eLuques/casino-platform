using Casino.Application.DTOs.Session;

namespace Casino.Application.Services;

public interface ISessionService
{
    Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request);
    Task<GetSessionResponse?> GetSessionAsync(Guid sessionId);
    Task<bool> ExpireSessionAsync(Guid sessionId);
    Task<bool> CloseSessionAsync(Guid sessionId);
    Task<IEnumerable<GetSessionResponse>> GetActiveSessionsAsync(Guid playerId);
}