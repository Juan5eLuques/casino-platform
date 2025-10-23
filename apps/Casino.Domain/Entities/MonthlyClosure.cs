namespace Casino.Domain.Entities;

/// <summary>
/// Representa un cierre mensual con snapshot de KPIs
/// </summary>
public class MonthlyClosure
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public Guid? UserId { get; set; }  // null = cierre consolidado de brand
    
    // Período
    public int PeriodMonth { get; set; }  // 1-12
    public int PeriodYear { get; set; }   // 2024+
    
    // KPIs de gaming
    public long TotalHandle { get; set; } = 0;  // Total apostado
    public long TotalPayouts { get; set; } = 0;  // Total pagado en premios
    public long GrossGamingRevenue { get; set; } = 0;  // HANDLE - PAYOUTS
    
    // Comisiones
    public long TotalCommissionsPaid { get; set; } = 0;
    
    // Control de cierre
    public string ClosureStatus { get; set; } = "PENDING";  // PENDING, PROCESSING, COMPLETED, FAILED
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    
    // Auditoría
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public BackofficeUser? User { get; set; }
    public BackofficeUser? ClosedByUser { get; set; }
}
