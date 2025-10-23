using Casino.Application.DTOs.Dashboard;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly CasinoDbContext _db;
    private readonly IHierarchyService _hierarchyService;
    private readonly ILogger<DashboardService> _logger;
    
    public DashboardService(
 CasinoDbContext db,
        IHierarchyService hierarchyService,
        ILogger<DashboardService> logger)
    {
        _db = db;
        _hierarchyService = hierarchyService;
        _logger = logger;
    }
    
    public async Task<FinancesSummaryResponse> GetFinancesSummaryAsync(
        DashboardQuery query,
        Guid currentUserId,
     string currentRole,
        CancellationToken cancellationToken = default)
    {
   var (from, to) = NormalizeDates(query);
    var (brandId, userIds) = await ResolveScopeAsync(query, currentUserId, currentRole, cancellationToken);
        
// Obtener player IDs en scope
        var playerIds = await GetPlayerIdsInScopeAsync(userIds, brandId, cancellationToken);
        
        // 1. Balance actual
      var balanceActual = await CalculateCurrentBalanceAsync(userIds, playerIds, brandId, cancellationToken);
     
        // 2. Delta del día
        var deltaDelDia = await CalculateDailyDeltaAsync(userIds, playerIds, brandId, from, cancellationToken);
        
     // 3. Cargas (TRANSFER hacia PLAYER desde BACKOFFICE)
     var cargas = await CalculateInternalTopupsAsync(brandId, from, to, userIds, cancellationToken);
        
      // 4. Depósitos A (MINT hacia HOUSE/ADMIN - emisión de fondos)
        var depositosA = await CalculateDepositsToAdminAsync(brandId, from, to, userIds, cancellationToken);
        
        // 5. Retiros (WITHDRAWAL/BURN desde el sistema)
  var retiros = await CalculateWithdrawalsAsync(brandId, from, to, cancellationToken);

        return new FinancesSummaryResponse
        {
            Period = new PeriodInfo { From = from, To = to, Timezone = query.Timezone },
  Scope = new ScopeInfo
            {
     Type = query.Scope.ToString(),
          UserId = currentUserId,
           BrandId = brandId
       },
            Fichas = new FichasInfo
{
     BalanceActual = balanceActual.Total,
         DeltaDelDia = deltaDelDia,
      Breakdown = new BalanceBreakdown
    {
  HouseBalance = balanceActual.House,
        CashiersBalance = balanceActual.Cashiers,
               PlayersBalance = balanceActual.Players
 }
            },
            Cargas = cargas,
       DepositosA = depositosA,
            Retiros = retiros,
  Links = new Dictionary<string, string>
         {
                ["reporteMensual"] = $"/api/v1/admin/reports/finances/monthly?year={from.Year}&month={from.Month}"
          }
     };
    }
    
public async Task<CasinoSummaryResponse> GetCasinoSummaryAsync(
   DashboardQuery query,
     Guid currentUserId,
        string currentRole,
        CancellationToken cancellationToken = default)
    {
 var (from, to) = NormalizeDates(query);
  var (brandId, userIds) = await ResolveScopeAsync(query, currentUserId, currentRole, cancellationToken);
        var playerIds = await GetPlayerIdsInScopeAsync(userIds, brandId, cancellationToken);
   
        // Calcular KPIs de casino desde Ledger
        var casinoStats = await _db.Ledger
          .Where(l => playerIds.Contains(l.PlayerId)
              && l.BrandId == brandId
         && l.CreatedAt >= from
          && l.CreatedAt <= to
   && (l.Reason == LedgerReason.BET || l.Reason == LedgerReason.WIN))
   .GroupBy(l => 1)
            .Select(g => new
            {
                Jugado = g.Where(l => l.Reason == LedgerReason.BET).Sum(l => l.DeltaBigint),
        Pagado = g.Where(l => l.Reason == LedgerReason.WIN).Sum(l => l.DeltaBigint),
     RondasTotales = g.Select(l => l.RoundId).Distinct().Count(),
     JugadoresActivos = g.Select(l => l.PlayerId).Distinct().Count()
  })
 .FirstOrDefaultAsync(cancellationToken);
        
        var jugado = casinoStats?.Jugado ?? 0;
        var pagado = casinoStats?.Pagado ?? 0;
        var netwin = jugado - pagado;
        
        // Calcular comisión promedio del árbol
      var comisionPorcentaje = await CalculateAverageCommissionRateAsync(currentUserId, cancellationToken);
        
      // Comisiones acumuladas pendientes del período
     var comisionesAcumuladas = await CalculatePendingCommissionsAsync(userIds, from, to, cancellationToken);
   
        // Comisión estimada del NetWin actual (si no hay acumuladas)
   var comisionEstimada = comisionesAcumuladas > 0 
   ? comisionesAcumuladas 
  : (long)(netwin * (comisionPorcentaje / 100m));
        
        var totalAPagar = netwin - comisionEstimada;
   
        var holdPercentage = jugado > 0 ? (decimal)netwin / jugado * 100 : 0;
        var apuestaPromedio = casinoStats?.RondasTotales > 0 
  ? jugado / casinoStats.RondasTotales 
 : 0;
        
        return new CasinoSummaryResponse
        {
  Period = new PeriodInfo { From = from, To = to, Timezone = query.Timezone },
  Jugado = jugado,
   Pagado = pagado,
     Netwin = netwin,
    ComisionPorcentaje = comisionPorcentaje,
   Comision = comisionEstimada, // Usar comisión real o estimada
         TotalAPagar = totalAPagar,
   KPIs = new CasinoKPIs
{
 HoldPercentage = Math.Round(holdPercentage, 2),
     RondasTotales = casinoStats?.RondasTotales ?? 0,
    ApuestaPromedio = apuestaPromedio,
  JugadoresActivos = casinoStats?.JugadoresActivos ?? 0
},
  Links = new Dictionary<string, string>
    {
     ["reporteMensual"] = $"/api/v1/admin/reports/casino/monthly?year={from.Year}&month={from.Month}"
   }
        };
    }
    
    public async Task<UsersCountsResponse> GetUsersCountsAsync(
        DashboardQuery query,
        Guid currentUserId,
        string currentRole,
     CancellationToken cancellationToken = default)
    {
        var (brandId, userIds) = await ResolveScopeAsync(query, currentUserId, currentRole, cancellationToken);
        
    // Jugadores directos (creados por el usuario actual)
    var jugadoresDirectos = await _db.Players
            .Where(p => p.CreatedByUserId == currentUserId && p.BrandId == brandId)
  .CountAsync(cancellationToken);
        
  // Agentes directos (creados por el usuario actual)
        var agentesDirectos = await _db.BackofficeUsers
  .Where(u => u.ParentAdminId == currentUserId && u.Role == BackofficeUserRole.CASHIER)
 .CountAsync(cancellationToken);
        
        // Total jugadores en árbol
        var totalJugadores = await _db.Players
   .Where(p => userIds.Contains(p.CreatedByUserId ?? Guid.Empty) && p.BrandId == brandId)
  .CountAsync(cancellationToken);
    
        // Total agentes en árbol
        var totalAgentes = await _db.BackofficeUsers
   .Where(u => userIds.Contains(u.Id) && u.Role == BackofficeUserRole.CASHIER)
            .CountAsync(cancellationToken);
        
        // Breakdown
        var jugadoresActivos = await _db.Players
          .Where(p => userIds.Contains(p.CreatedByUserId ?? Guid.Empty) 
    && p.BrandId == brandId 
        && p.Status == PlayerStatus.ACTIVE)
            .CountAsync(cancellationToken);
        
      var jugadoresInactivos = totalJugadores - jugadoresActivos;
   
   // Agentes por nivel
      var agentesPorNivel = await _db.BackofficeUsers
     .Where(u => userIds.Contains(u.Id) && u.Role == BackofficeUserRole.CASHIER)
     .GroupBy(u => u.HierarchyLevel)
            .Select(g => new { Level = g.Key, Count = g.Count() })
 .ToDictionaryAsync(x => $"nivel{x.Level}", x => x.Count, cancellationToken);
        
     return new UsersCountsResponse
      {
            JugadoresDirectos = jugadoresDirectos,
            AgentesDirectos = agentesDirectos,
            TotalJugadores = totalJugadores,
        TotalAgentes = totalAgentes,
       Breakdown = new UsersBreakdown
     {
 JugadoresActivos = jugadoresActivos,
        JugadoresInactivos = jugadoresInactivos,
    AgentesPorNivel = agentesPorNivel
            }
        };
    }
    
    public async Task<AlertsSummaryResponse> GetAlertsAsync(
  DashboardQuery query,
      Guid currentUserId,
  string currentRole,
        CancellationToken cancellationToken = default)
    {
        var (brandId, userIds) = await ResolveScopeAsync(query, currentUserId, currentRole, cancellationToken);
        
     var alertas = new List<Alert>();
        
        // Float bajo de cajeros
        var floatThreshold = 10000;
        var cajerosFloatBajo = await _db.BackofficeUsers
       .Where(u => userIds.Contains(u.Id) 
        && u.Role == BackofficeUserRole.CASHIER 
       && u.WalletBalance < floatThreshold)
   .CountAsync(cancellationToken);
      
        if (cajerosFloatBajo > 0)
        {
            alertas.Add(new Alert
        {
        Tipo = "FLOAT_BAJO",
     Severidad = "HIGH",
 Count = cajerosFloatBajo,
         Mensaje = $"{cajerosFloatBajo} cajeros con saldo < {floatThreshold}"
            });
        }
        
        // Estado operativo
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);
        
        var cajerosActivos = await _db.WalletTransactions
  .Where(t => userIds.Contains(t.CreatedByUserId)
              && t.CreatedAt >= last24h
   && t.CreatedByRole == "CASHIER")
          .Select(t => t.CreatedByUserId)
     .Distinct()
            .CountAsync(cancellationToken);
 
        var floatTotal = await _db.BackofficeUsers
   .Where(u => userIds.Contains(u.Id) && u.Role == BackofficeUserRole.CASHIER)
 .SumAsync(u => u.WalletBalance, cancellationToken);
        
        // Jugadores online (con sesión activa)
        var jugadoresOnline = await _db.GameSessions
      .Where(s => s.Status == GameSessionStatus.OPEN)
            .Join(_db.Players, 
     s => s.PlayerId, 
       p => p.Id, 
     (s, p) => p)
     .Where(p => userIds.Contains(p.CreatedByUserId ?? Guid.Empty))
 .CountAsync(cancellationToken);
    
        return new AlertsSummaryResponse
   {
            Alertas = alertas,
            EstadoOperativo = new OperationalStatus
  {
       CajerosActivos = cajerosActivos,
     JugadoresOnline = jugadoresOnline,
     FloatTotal = (long)floatTotal,
        TransaccionesPendientes = 0  // Implementar si tienes estados
 }
        };
    }
    
    public async Task<DashboardOverviewResponse> GetOverviewAsync(
        DashboardQuery query,
        Guid currentUserId,
        string currentRole,
      CancellationToken cancellationToken = default)
    {
        // Ejecutar todas las queries SECUENCIALMENTE para evitar problemas de concurrencia con DbContext
        // DbContext no es thread-safe y no debe usarse en paralelo
        var finanzas = await GetFinancesSummaryAsync(query, currentUserId, currentRole, cancellationToken);
    var casino = await GetCasinoSummaryAsync(query, currentUserId, currentRole, cancellationToken);
 var usuarios = await GetUsersCountsAsync(query, currentUserId, currentRole, cancellationToken);
    var alertas = await GetAlertsAsync(query, currentUserId, currentRole, cancellationToken);
        
        return new DashboardOverviewResponse
        {
   Finanzas = finanzas,
   Casino = casino,
        Usuarios = usuarios,
            Alertas = alertas
  };
    }
    
    // === HELPER METHODS ===
    
    private (DateTime from, DateTime to) NormalizeDates(DashboardQuery query)
    {
        var now = DateTime.UtcNow;
  var from = query.From ?? now.Date; // Hoy 00:00
      var to = query.To ?? now.Date.AddDays(1).AddTicks(-1); // Hoy 23:59:59
        return (from, to);
    }
    
    private async Task<(Guid brandId, HashSet<Guid> userIds)> ResolveScopeAsync(
      DashboardQuery query,
        Guid currentUserId,
string currentRole,
  CancellationToken cancellationToken)
    {
        // Determinar brandId
        var brandId = query.BrandId ?? Guid.Empty; // Se validará en el endpoint
     
        // Determinar userIds en scope
        HashSet<Guid> userIds;
        
      switch (query.Scope)
    {
        case DashboardScope.DIRECT:
       userIds = new HashSet<Guid> { currentUserId };
          break;
      
            case DashboardScope.TREE:
        var descendants = await _hierarchyService.GetDescendantsAsync(currentUserId, cancellationToken);
         userIds = descendants.Select(d => d.Id).Append(currentUserId).ToHashSet();
   
      // SONNET FIX: Para SUPER_ADMIN sin descendientes, incluir todos los usuarios del brand
    if (userIds.Count == 1 && currentRole == "SUPER_ADMIN")
      {
            _logger.LogWarning("SUPER_ADMIN {UserId} has no descendants in TREE scope, falling back to GLOBAL", currentUserId);
     var allBrandUsers = await _db.BackofficeUsers
   .Where(u => u.BrandId == brandId)
        .Select(u => u.Id)
 .ToListAsync(cancellationToken);
        userIds = allBrandUsers.ToHashSet();
     }
      break;
  
            case DashboardScope.GLOBAL:
          // Todos los usuarios del brand
      var globalUsers = await _db.BackofficeUsers
.Where(u => u.BrandId == brandId)
             .Select(u => u.Id)
   .ToListAsync(cancellationToken);
      userIds = globalUsers.ToHashSet();
                break;
     
    default:
      userIds = new HashSet<Guid> { currentUserId };
        break;
        }
        
        _logger.LogInformation("Scope resolved: {Scope}, UserIds count: {Count}, BrandId: {BrandId}", 
            query.Scope, userIds.Count, brandId);
     
        return (brandId, userIds);
    }
    
    private async Task<HashSet<Guid>> GetPlayerIdsInScopeAsync(
        HashSet<Guid> userIds,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        // SONNET FIX: Para scope amplio (muchos usuarios), incluir TODOS los players del brand
        // Esto es más eficiente y correcto para GLOBAL scope
  HashSet<Guid> playerIds;
        
        if (userIds.Count > 50) // Threshold: si hay más de 50 usuarios, probablemente es GLOBAL
     {
            var allPlayers = await _db.Players
          .Where(p => p.BrandId == brandId)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
  
            playerIds = allPlayers.ToHashSet();
            
          _logger.LogInformation("GetPlayerIdsInScopeAsync: Using GLOBAL mode - {Count} players in brand {BrandId}", 
      playerIds.Count, brandId);
    }
    else
   {
            var playerList = await _db.Players
          .Where(p => userIds.Contains(p.CreatedByUserId ?? Guid.Empty) && p.BrandId == brandId)
                .Select(p => p.Id)
       .ToListAsync(cancellationToken);
            
      playerIds = playerList.ToHashSet();
        
 _logger.LogInformation("GetPlayerIdsInScopeAsync: Using TREE/DIRECT mode - {Count} players created by {UserCount} users", 
    playerIds.Count, userIds.Count);
        }
        
        return playerIds;
    }
    
    private async Task<(long Total, long House, long Cashiers, long Players)> CalculateCurrentBalanceAsync(
    HashSet<Guid> userIds,
        HashSet<Guid> playerIds,
        Guid brandId,
        CancellationToken cancellationToken)
    {
        var houseBalance = await _db.BackofficeUsers
 .Where(u => userIds.Contains(u.Id) 
          && (u.Role == BackofficeUserRole.BRAND_ADMIN || u.Role == BackofficeUserRole.SUPER_ADMIN))
            .SumAsync(u => u.WalletBalance, cancellationToken);
        
        var cashiersBalance = await _db.BackofficeUsers
            .Where(u => userIds.Contains(u.Id) && u.Role == BackofficeUserRole.CASHIER)
            .SumAsync(u => u.WalletBalance, cancellationToken);
        
        // FIX: Usar Players.WalletBalance (decimal) en lugar de Wallets.BalanceBigint (bigint obsoleto)
        var playersBalance = await _db.Players
.Where(p => playerIds.Contains(p.Id))
    .SumAsync(p => p.WalletBalance, cancellationToken);
        
 var total = (long)houseBalance + (long)cashiersBalance + (long)playersBalance;
        
    return (total, (long)houseBalance, (long)cashiersBalance, (long)playersBalance);
 }
    
    private async Task<long> CalculateDailyDeltaAsync(
  HashSet<Guid> userIds,
        HashSet<Guid> playerIds,
        Guid brandId,
     DateTime from,
        CancellationToken cancellationToken)
    {
        // Calcular el cambio neto en balances desde el inicio del día
   // Suma todas las transacciones que afectan a usuarios/players en scope
 
     // Delta de transacciones hacia usuarios en scope
   var deltaToUsers = await _db.WalletTransactions
      .Where(t => t.BrandId == brandId
  && t.CreatedAt >= from
        && t.CreatedAt < from.AddDays(1) // Solo hasta fin del día
       && userIds.Contains(t.ToUserId))
  .SumAsync(t => t.Amount, cancellationToken);
   
     // Delta de transacciones desde usuarios en scope
        var deltaFromUsers = await _db.WalletTransactions
  .Where(t => t.BrandId == brandId
            && t.CreatedAt >= from
     && t.CreatedAt < from.AddDays(1)
       && t.FromUserId.HasValue 
       && userIds.Contains(t.FromUserId.Value))
     .SumAsync(t => t.Amount, cancellationToken);
        
        // Delta neto = entradas - salidas
        return (long)(deltaToUsers - deltaFromUsers);
    }
    
    private async Task<TransactionSummary> CalculateTransactionSummaryAsync(
        Guid brandId,
        DateTime from,
        DateTime to,
        TransactionType type,
        string toUserType,
CancellationToken cancellationToken)
    {
        var transactions = await _db.WalletTransactions
            .Where(t => t.BrandId == brandId
       && t.TransactionType == type
                && t.ToUserType == toUserType
       && t.CreatedAt >= from
     && t.CreatedAt <= to)
      .GroupBy(t => 1)
            .Select(g => new
          {
 Total = g.Sum(t => t.Amount),
            Count = g.Count()
     })
  .FirstOrDefaultAsync(cancellationToken);
        
        var total = (long)(transactions?.Total ?? 0);
        var count = transactions?.Count ?? 0;
        var promedio = count > 0 ? total / count : 0;
        
        return new TransactionSummary
 {
   Total = total,
Count = count,
      Promedio = promedio
        };
    }
    
    private async Task<TransactionSummary> CalculateWithdrawalsAsync(
      Guid brandId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var transactions = await _db.WalletTransactions
   .Where(t => t.BrandId == brandId
    && (t.TransactionType == TransactionType.WITHDRAWAL || t.TransactionType == TransactionType.BURN)
    && t.CreatedAt >= from
         && t.CreatedAt <= to)
            .GroupBy(t => 1)
            .Select(g => new
            {
Total = g.Sum(t => t.Amount),
       Count = g.Count()
     })
  .FirstOrDefaultAsync(cancellationToken);

        var total = (long)(transactions?.Total ?? 0);
        var count = transactions?.Count ?? 0;
      var promedio = count > 0 ? total / count : 0;
     
        return new TransactionSummary
        {
      Total = total,
  Count = count,
          Promedio = promedio
    };
}
    
    private async Task<decimal> CalculateAverageCommissionRateAsync(
        Guid userId,
    CancellationToken cancellationToken)
    {
        // FIX: Calcular la comisión total del árbol (upstream flow)
    // La comisión se calcula sumando las comisiones de TODOS los subordinados
 // porque el admin superior recibe parte de las comisiones de sus subordinados
        
        var user = await _db.BackofficeUsers
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

      if (user == null) return 0;
     
        // Obtener todos los usuarios del árbol jerárquico
var hierarchyUsers = await _db.BackofficeUsers
      .Where(u => u.HierarchyPath != null && u.HierarchyPath.Contains(userId.ToString()))
         .ToListAsync(cancellationToken);
     
        // Incluir al usuario actual si tiene comisión
        if (user.CommissionPercent > 0)
     {
     hierarchyUsers.Add(user);
      }
   
    // Si no hay usuarios con comisión en el árbol, retornar 0
      if (hierarchyUsers.Count == 0) return 0;
        
        // Calcular el promedio ponderado de comisiones del árbol
        // En un sistema real, esto sería la suma de comisiones acumuladas
        var averageCommission = hierarchyUsers.Average(u => u.CommissionPercent);
        
        _logger.LogInformation(
    "Commission calculated for user {UserId}: {Commission}% (based on {Count} users in hierarchy)",
   userId, Math.Round(averageCommission, 2), hierarchyUsers.Count);
        
    return Math.Round(averageCommission, 2);
}
    
    /// <summary>
    /// Calcula cargas internas (PLAYER_TOPUP_INTERNAL): BACKOFFICE ? PLAYER
    /// </summary>
    private async Task<TransactionSummary> CalculateInternalTopupsAsync(
        Guid brandId,
    DateTime from,
        DateTime to,
   HashSet<Guid> userIds,
        CancellationToken cancellationToken)
    {
   var transactions = await _db.WalletTransactions
            .Where(t => t.BrandId == brandId
                && t.TransactionType == TransactionType.TRANSFER
       && t.FromUserType == "BACKOFFICE" // Desde backoffice
 && t.ToUserType == "PLAYER" // Hacia player
              && userIds.Contains(t.CreatedByUserId) // Creado por usuarios en scope
                && t.CreatedAt >= from
         && t.CreatedAt <= to)
.GroupBy(t => 1)
       .Select(g => new
            {
      Total = g.Sum(t => t.Amount),
       Count = g.Count()
            })
  .FirstOrDefaultAsync(cancellationToken);
     
        var total = (long)(transactions?.Total ?? 0);
        var count = transactions?.Count ?? 0;
        var promedio = count > 0 ? total / count : 0;
    
        return new TransactionSummary
        {
      Total = total,
  Count = count,
  Promedio = promedio
};
    }
    
    /// <summary>
    /// Calcula depósitos hacia HOUSE/ADMIN (MINT o transferencias externas)
    /// </summary>
    private async Task<TransactionSummary> CalculateDepositsToAdminAsync(
        Guid brandId,
   DateTime from,
     DateTime to,
     HashSet<Guid> userIds,
        CancellationToken cancellationToken)
    {
// Buscar MINTs hacia usuarios BACKOFFICE en scope (HOUSE/ADMIN)
 var transactions = await _db.WalletTransactions
          .Where(t => t.BrandId == brandId
                && t.TransactionType == TransactionType.MINT
           && t.ToUserType == "BACKOFFICE"
      && userIds.Contains(t.ToUserId) // Hacia usuarios en scope
       && t.CreatedAt >= from
   && t.CreatedAt <= to)
   .GroupBy(t => 1)
          .Select(g => new
       {
  Total = g.Sum(t => t.Amount),
     Count = g.Count()
      })
   .FirstOrDefaultAsync(cancellationToken);
        
        var total = (long)(transactions?.Total ?? 0);
        var count = transactions?.Count ?? 0;
        var promedio = count > 0 ? total / count : 0;
   
        return new TransactionSummary
        {
   Total = total,
        Count = count,
   Promedio = promedio
        };
    }
  
    /// <summary>
    /// Calcula las comisiones pendientes acumuladas en el período
    /// </summary>
    private async Task<long> CalculatePendingCommissionsAsync(
   HashSet<Guid> userIds,
    DateTime from,
  DateTime to,
   CancellationToken cancellationToken)
    {
   // Sumar comisiones pendientes (no liquidadas) de usuarios en scope
        var pendingCommissions = await _db.CommissionAccruals
 .Where(ca => userIds.Contains(ca.UserId)
     && !ca.Settled
          && ca.CreatedAt >= from
   && ca.CreatedAt <= to)
       .SumAsync(ca => ca.CommissionAmount, cancellationToken);
        
        return pendingCommissions;
}
}
