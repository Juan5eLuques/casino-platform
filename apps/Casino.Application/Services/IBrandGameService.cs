using Casino.Application.DTOs.BrandGame;

namespace Casino.Application.Services;

public interface IBrandGameService
{
    Task<BrandGameResponse> AssignGameToBrandAsync(Guid brandId, AssignGameToBrandRequest request, Guid currentUserId, Guid? operatorScope = null);
    Task<GetBrandGamesResponse> GetBrandGamesAsync(Guid brandId, Guid? operatorScope = null);
    Task<BrandGameResponse> UpdateBrandGameAsync(Guid brandId, Guid gameId, UpdateBrandGameRequest request, Guid currentUserId, Guid? operatorScope = null);
    Task<bool> RemoveGameFromBrandAsync(Guid brandId, Guid gameId, Guid currentUserId, Guid? operatorScope = null);
}