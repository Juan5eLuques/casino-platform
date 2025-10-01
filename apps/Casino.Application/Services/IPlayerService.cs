using Casino.Application.DTOs.Player;

namespace Casino.Application.Services;

public interface IPlayerService
{
    Task<GetPlayerResponse> CreatePlayerAsync(CreatePlayerRequest request, Guid currentUserId);
    Task<QueryPlayersResponse> GetPlayersAsync(QueryPlayersRequest request, Guid? operatorScope = null, Guid? brandScope = null);
    Task<GetPlayerResponse?> GetPlayerAsync(Guid playerId, Guid? operatorScope = null, Guid? brandScope = null);
    Task<GetPlayerResponse> UpdatePlayerAsync(Guid playerId, UpdatePlayerRequest request, Guid currentUserId, Guid? operatorScope = null, Guid? brandScope = null);
    Task<WalletAdjustmentResponse> AdjustPlayerWalletAsync(Guid playerId, AdjustPlayerWalletRequest request, Guid currentUserId, Guid? operatorScope = null, Guid? brandScope = null);
}