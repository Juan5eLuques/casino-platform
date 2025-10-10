using Casino.Application.DTOs.Player;
using Casino.Domain.Enums;

namespace Casino.Application.Services;

public interface IPlayerService
{
    /// <summary>
    /// Crea un nuevo player en el brand especificado.
    /// Incluye informaci�n del rol del usuario actual para vincular correctamente con cashiers.
    /// </summary>
    Task<GetPlayerResponse> CreatePlayerAsync(CreatePlayerRequest request, Guid currentUserId, Guid effectiveBrandId, BackofficeUserRole? currentUserRole = null);
    
    /// <summary>
    /// Obtiene players con paginaci�n y filtros. Scope por brand autom�tico.
    /// </summary>
    Task<QueryPlayersResponse> GetPlayersAsync(QueryPlayersRequest request, Guid? brandScope = null);
    
    /// <summary>
    /// Obtiene un player espec�fico. Scope por brand autom�tico.
    /// </summary>
    Task<GetPlayerResponse?> GetPlayerAsync(Guid playerId, Guid? brandScope = null);
    
    /// <summary>
    /// Actualiza un player existente. Scope por brand autom�tico.
    /// </summary>
    Task<GetPlayerResponse> UpdatePlayerAsync(Guid playerId, UpdatePlayerRequest request, Guid currentUserId, Guid? brandScope = null);
    
    /// <summary>
    /// Ajusta el balance del wallet de un player. Scope por brand autom�tico.
    /// </summary>
    Task<WalletAdjustmentResponse> AdjustPlayerWalletAsync(Guid playerId, AdjustPlayerWalletRequest request, Guid currentUserId, Guid? brandScope = null);
    
    /// <summary>
    /// Elimina un player. Scope por brand autom�tico.
    /// </summary>
    Task<bool> DeletePlayerAsync(Guid playerId, Guid currentUserId, Guid? brandScope = null);
}