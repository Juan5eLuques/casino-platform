using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

public class BackofficeUser
{
    public Guid Id { get; set; }
    public Guid? BrandId { get; set; } // null solo para SUPER_ADMIN
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public BackofficeUserRole Role { get; set; } // SUPER_ADMIN, BRAND_ADMIN, CASHIER
    public BackofficeUserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Hierarchical structure for cashiers
    public Guid? ParentCashierId { get; set; }
    
    // SONNET: Renombrado de CommissionRate a CommissionPercent (0-100) para consistencia
    public decimal CommissionPercent { get; set; } = 0; // Porcentaje de comisión sobre cashiers subordinados (0-100)

    /// <summary>
    /// ID del usuario que creó este usuario de backoffice
    /// </summary>
    public Guid? CreatedByUserId { get; set; }
    
    // SONNET: Rol del usuario que creó este usuario (para auditoría)
    /// <summary>
    /// Rol del usuario que creó este usuario de backoffice
    /// </summary>
    public string? CreatedByRole { get; set; }
    
    /// <summary>
    /// Balance del wallet para operaciones internas (formato decimal)
    /// </summary>
    public decimal WalletBalance { get; set; } = 0.00m;

    // Navigation properties
    public Brand? Brand { get; set; }
    public BackofficeUser? ParentCashier { get; set; }
    public ICollection<BackofficeUser> SubordinateCashiers { get; set; } = new List<BackofficeUser>();
    public ICollection<CashierPlayer> CashierPlayers { get; set; } = new List<CashierPlayer>();
    public ICollection<BackofficeAudit> BackofficeAudits { get; set; } = new List<BackofficeAudit>();
    
    /// <summary>
    /// Usuario que creó este usuario de backoffice (si aplica)
    /// </summary>
    public BackofficeUser? CreatedByUser { get; set; }
}