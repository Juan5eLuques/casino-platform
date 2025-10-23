namespace Casino.Domain.Entities;

/// <summary>
/// Representa una acumulación de comisión pendiente de liquidación mensual
/// </summary>
public class CommissionAccrual
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentUserId { get; set; }
    
    // Período
    public int PeriodMonth { get; set; }  // 1-12
    public int PeriodYear { get; set; }   // 2024+
    
    // Cálculo de comisión
    public long BaseAmount { get; set; }  // Monto base (ej: NetWin del nivel inferior)
    public decimal CommissionRate { get; set; }  // Tasa de comisión (0.0 - 1.0)
    public long CommissionAmount { get; set; }  // Monto de comisión calculado
    
    // Liquidación
    public bool Settled { get; set; } = false;
    public DateTime? SettledAt { get; set; }
    public Guid? SettledTransactionId { get; set; }
    
    // Origen (qué generó esta comisión)
    public string? SourceType { get; set; }  // "NETWIN", "TRANSFER_FEE", etc.
    public Guid? SourceTransactionId { get; set; }
    public Guid? SourceRoundId { get; set; }
    public Guid? SourcePlayerId { get; set; }
    
    // Auditoría
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public BackofficeUser User { get; set; } = null!;
    public BackofficeUser? ParentUser { get; set; }
    public WalletTransaction? SettledTransaction { get; set; }
    public WalletTransaction? SourceTransaction { get; set; }
    public Round? SourceRound { get; set; }
    public Player? SourcePlayer { get; set; }
}
