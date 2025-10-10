using Casino.Application.DTOs.Cashier;
using Casino.Application.DTOs.Player;

namespace Casino.Application.Services;

public interface ICashierPlayerService
{
    Task<AssignPlayerToCashierResponse> AssignPlayerAsync(Guid cashierId, Guid playerId, Guid currentUserId);
    Task<GetCashierPlayersResponse> GetCashierPlayersAsync(Guid cashierId, Guid? brandScope);
    Task<GetPlayerCashiersResponse> GetPlayerCashiersAsync(Guid playerId, Guid? brandScope);
    Task<UnassignPlayerResponse> UnassignPlayerAsync(Guid cashierId, Guid playerId, Guid currentUserId, Guid? brandScope);
    
    // Nuevos métodos para el sistema brand-only
    Task<QueryPlayersResponse> GetCashierPlayersAsync(Guid cashierId, int page = 1, int pageSize = 20);
    Task<bool> IsPlayerAssignedToCashierAsync(Guid cashierId, Guid playerId);
}