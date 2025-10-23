using Casino.Application.DTOs.Dashboard;

namespace Casino.Application.Services;

/// <summary>
/// Servicio para generar datos del dashboard de backoffice
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Obtiene el resumen financiero del período
    /// </summary>
    Task<FinancesSummaryResponse> GetFinancesSummaryAsync(
    DashboardQuery query,
        Guid currentUserId,
    string currentRole,
     CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene el resumen de KPIs de casino
    /// </summary>
    Task<CasinoSummaryResponse> GetCasinoSummaryAsync(
        DashboardQuery query,
        Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene los conteos de usuarios
    /// </summary>
    Task<UsersCountsResponse> GetUsersCountsAsync(
        DashboardQuery query,
        Guid currentUserId,
        string currentRole,
   CancellationToken cancellationToken = default);

/// <summary>
    /// Obtiene las alertas operativas
    /// </summary>
    Task<AlertsSummaryResponse> GetAlertsAsync(
        DashboardQuery query,
 Guid currentUserId,
  string currentRole,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene la vista consolidada del dashboard
    /// </summary>
    Task<DashboardOverviewResponse> GetOverviewAsync(
        DashboardQuery query,
        Guid currentUserId,
  string currentRole,
        CancellationToken cancellationToken = default);
}
