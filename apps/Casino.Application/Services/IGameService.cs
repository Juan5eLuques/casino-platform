using Casino.Application.DTOs.Game;
using Casino.Application.Services.Models;

namespace Casino.Application.Services;

public interface IGameService
{
    Task<CreateGameResponse> CreateGameAsync(CreateGameRequest request);
    Task<IEnumerable<GetGameResponse>> GetGamesAsync(bool? enabled = null);
    Task<GetGameResponse?> GetGameAsync(Guid gameId);
    Task<bool> UpdateGameAsync(Guid gameId, UpdateGameRequest request);
    Task<bool> DeleteGameAsync(Guid gameId);
    
    // Brand-Game management
    Task<bool> AssignGameToBrandAsync(AssignGameToBrandRequest request);
    Task<bool> UnassignGameFromBrandAsync(Guid brandId, Guid gameId);
    Task<IEnumerable<GetBrandGameResult>> GetBrandGamesAsync(Guid brandId, bool? enabled = null);
    Task<bool> UpdateBrandGameAsync(UpdateBrandGameRequest request);
}