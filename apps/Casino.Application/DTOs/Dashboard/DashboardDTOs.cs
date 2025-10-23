namespace Casino.Application.DTOs.Dashboard;

/// <summary>
/// Parámetros comunes para todos los endpoints del dashboard
/// </summary>
public record DashboardQuery
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string Timezone { get; init; } = "UTC";
    public Guid? BrandId { get; init; }
    public DashboardScope Scope { get; init; } = DashboardScope.TREE;
    public string? Currency { get; init; }
}

public enum DashboardScope
{
    DIRECT,  // Solo entidades creadas directamente por el usuario
    TREE,    // Árbol completo del usuario (descendientes)
    GLOBAL   // Todo el brand (SUPER_ADMIN o BRAND_ADMIN)
}

/// <summary>
/// Respuesta del resumen financiero
/// </summary>
public record FinancesSummaryResponse
{
public PeriodInfo Period { get; init; } = null!;
    public ScopeInfo Scope { get; init; } = null!;
    public FichasInfo Fichas { get; init; } = null!;
    public TransactionSummary Cargas { get; init; } = null!;
    public TransactionSummary DepositosA { get; init; } = null!;
    public TransactionSummary Retiros { get; init; } = null!;
    public Dictionary<string, string> Links { get; init; } = new();
}

public record PeriodInfo
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string Timezone { get; init; } = "UTC";
}

public record ScopeInfo
{
    public string Type { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid? BrandId { get; init; }
}

public record FichasInfo
{
    public long BalanceActual { get; init; }
 public long DeltaDelDia { get; init; }
    public BalanceBreakdown Breakdown { get; init; } = null!;
}

public record BalanceBreakdown
{
    public long HouseBalance { get; init; }
    public long CashiersBalance { get; init; }
    public long PlayersBalance { get; init; }
}

public record TransactionSummary
{
public long Total { get; init; }
    public int Count { get; init; }
    public long Promedio { get; init; }
}

/// <summary>
/// Respuesta del resumen de casino
/// </summary>
public record CasinoSummaryResponse
{
    public PeriodInfo Period { get; init; } = null!;
public long Jugado { get; init; }
    public long Pagado { get; init; }
    public long Netwin { get; init; }
public decimal ComisionPorcentaje { get; init; }
    public long Comision { get; init; }
    public long TotalAPagar { get; init; }
    public CasinoKPIs KPIs { get; init; } = null!;
    public Dictionary<string, string> Links { get; init; } = new();
}

public record CasinoKPIs
{
    public decimal HoldPercentage { get; init; }
 public int RondasTotales { get; init; }
    public long ApuestaPromedio { get; init; }
    public int JugadoresActivos { get; init; }
}

/// <summary>
/// Respuesta de conteos de usuarios
/// </summary>
public record UsersCountsResponse
{
    public int JugadoresDirectos { get; init; }
    public int AgentesDirectos { get; init; }
    public int TotalJugadores { get; init; }
    public int TotalAgentes { get; init; }
    public UsersBreakdown Breakdown { get; init; } = null!;
}

public record UsersBreakdown
{
  public int JugadoresActivos { get; init; }
    public int JugadoresInactivos { get; init; }
    public Dictionary<string, int> AgentesPorNivel { get; init; } = new();
}

/// <summary>
/// Respuesta del overview consolidado
/// </summary>
public record DashboardOverviewResponse
{
    public FinancesSummaryResponse Finanzas { get; init; } = null!;
    public UsersCountsResponse Usuarios { get; init; } = null!;
    public CasinoSummaryResponse Casino { get; init; } = null!;
    public AlertsSummaryResponse Alertas { get; init; } = null!;
}

/// <summary>
/// Respuesta de alertas (simplificado para fase 1)
/// </summary>
public record AlertsSummaryResponse
{
    public List<Alert> Alertas { get; init; } = new();
    public OperationalStatus EstadoOperativo { get; init; } = null!;
}

public record Alert
{
    public string Tipo { get; init; } = string.Empty;
    public string Severidad { get; init; } = string.Empty;
    public int Count { get; init; }
    public long? Total { get; init; }
    public string? Link { get; init; }
    public string? Mensaje { get; init; }
}

public record OperationalStatus
{
    public int CajerosActivos { get; init; }
    public int JugadoresOnline { get; init; }
    public long FloatTotal { get; init; }
    public int TransaccionesPendientes { get; init; }
}
