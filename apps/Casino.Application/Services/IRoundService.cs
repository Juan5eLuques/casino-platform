using Casino.Application.DTOs.Session;

namespace Casino.Application.Services;

public interface IRoundService
{
    Task<CreateRoundResponse> CreateRoundAsync(CreateRoundRequest request);
    Task<GetRoundResponse?> GetRoundAsync(Guid roundId);
    Task<CloseRoundResponse> CloseRoundAsync(CloseRoundRequest request);
    Task<IEnumerable<GetRoundResponse>> GetSessionRoundsAsync(Guid sessionId);
}