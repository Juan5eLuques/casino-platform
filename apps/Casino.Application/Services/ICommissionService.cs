using Casino.Domain.Entities;

namespace Casino.Application.Services;

/// <summary>
/// Servicio para gestión de comisiones multinivel con acumulación mensual
/// </summary>
public interface ICommissionService
{
    /// <summary>
    /// Acumula comisiones para todo el árbol jerárquico basado en NetWin de un player.
    /// Calcula comisión en cascada: cashier ? sub-admin ? admin ? super-admin
    /// Las comisiones se acumulan pero NO se pagan inmediatamente (liquidación mensual)
    /// </summary>
    /// <param name="playerId">Player que generó el NetWin</param>
    /// <param name="netWinAmount">Monto del NetWin (HANDLE - PAYOUTS)</param>
    /// <param name="roundId">Round que generó el NetWin (opcional)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task AccrueCommissionsFromNetWinAsync(
        Guid playerId,
        long netWinAmount,
        Guid? roundId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Acumula comisión de una transacción específica (ej: transfer fee)
    /// </summary>
    /// <param name="transactionId">ID de la transacción que genera la comisión</param>
    /// <param name="userId">Usuario que recibe la comisión</param>
    /// <param name="commissionAmount">Monto de la comisión</param>
    /// <param name="commissionType">Tipo de comisión (ej: "TRANSFER_FEE")</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task AccrueCommissionFromTransactionAsync(
        Guid transactionId,
        Guid userId,
        long commissionAmount,
        string commissionType,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene comisiones pendientes de liquidación de un usuario para un período
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="year">Año del período</param>
    /// <param name="month">Mes del período</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de comisiones pendientes</returns>
    Task<IEnumerable<CommissionAccrual>> GetPendingCommissionsAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene el total de comisiones acumuladas (pendientes de liquidación) de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="year">Año del período</param>
    /// <param name="month">Mes del período</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Total en comisiones pendientes</returns>
    Task<long> GetPendingCommissionsTotalAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Liquida todas las comisiones pendientes de un período para un brand.
    /// Genera WalletTransactions de tipo COMMISSION para cada usuario.
    /// Marca las comisiones como "settled" (liquidadas).
    /// </summary>
    /// <param name="brandId">ID del brand</param>
    /// <param name="year">Año del período</param>
    /// <param name="month">Mes del período</param>
    /// <param name="settledByUserId">Usuario que ejecuta la liquidación</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la liquidación con detalles</returns>
    Task<CommissionSettlementResult> SettleCommissionsForPeriodAsync(
        Guid brandId,
        int year,
        int month,
        Guid settledByUserId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calcula cuánto le corresponde de comisión a cada nivel del árbol
    /// para un NetWin específico.
    /// Útil para preview antes de acumular.
    /// </summary>
    /// <param name="playerId">Player que generó el NetWin</param>
    /// <param name="netWinAmount">Monto del NetWin</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Breakdown de comisiones por usuario</returns>
    Task<CommissionBreakdown> CalculateCommissionBreakdownAsync(
        Guid playerId,
        long netWinAmount,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de la liquidación de comisiones
/// </summary>
public class CommissionSettlementResult
{
    public bool Success { get; set; }
    public int TotalUsersSettled { get; set; }
    public long TotalAmountSettled { get; set; }
    public List<UserSettlement> UserSettlements { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Detalle de liquidación por usuario
/// </summary>
public class UserSettlement
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public long TotalCommission { get; set; }
    public Guid TransactionId { get; set; }
    public int CommissionCount { get; set; }
}

/// <summary>
/// Breakdown de comisiones por nivel
/// </summary>
public class CommissionBreakdown
{
    public Guid PlayerId { get; set; }
    public long NetWinAmount { get; set; }
    public List<LevelCommission> Levels { get; set; } = new();
    public long TotalCommissions => Levels.Sum(l => l.EffectiveCommission);
}

/// <summary>
/// Comisión de un nivel específico
/// </summary>
public class LevelCommission
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int HierarchyLevel { get; set; }
    public decimal CommissionRate { get; set; }  // 0.0 - 1.0
    public long CommissionAmount { get; set; }  // NetWin * Rate
    public long EffectiveCommission { get; set; }  // Después de restar niveles inferiores
}
