using Casino.Application.DTOs.Balance;

namespace Casino.Application.Services;

/// <summary>
/// Servicio para obtener el balance del usuario logueado
/// </summary>
public interface IBalanceService
{
    /// <summary>
  /// Obtiene el balance del usuario logueado automáticamente
    /// </summary>
  Task<UserBalanceResponse> GetMyBalanceAsync(Guid userId, string userType);
}
