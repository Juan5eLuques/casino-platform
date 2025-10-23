using Casino.Application.Providers;
using Casino.Domain.Entities;

namespace Casino.Application.Services;

/// <summary>
/// Servicio para lanzar juegos y gestionar launch logs
/// </summary>
public interface IGameLaunchService
{
 /// <summary>
    /// Lanza un juego para un jugador en un brand específico
    /// </summary>
    Task<GameLaunchResponse> LaunchGameAsync(
     string gameCode, 
 Guid playerId, 
   Guid brandId, 
        bool isDemo = false,
     CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Obtiene el log de un launch específico por sessionId
    /// </summary>
    Task<GameLaunchLog?> GetLaunchLogAsync(
    Guid sessionId, 
 CancellationToken cancellationToken = default);
  
 /// <summary>
 /// Obtiene los logs de launch de un jugador
    /// </summary>
 Task<IEnumerable<GameLaunchLog>> GetPlayerLaunchLogsAsync(
 Guid playerId,
        int limit = 10,
 CancellationToken cancellationToken = default);
}
