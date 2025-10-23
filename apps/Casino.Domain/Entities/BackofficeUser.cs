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
    
    // Hierarchical structure for cashiers (DEPRECATED - use ParentAdminId)
    public Guid? ParentCashierId { get; set; }
    
    // NEW: Multilevel hierarchy support
    /// <summary>
    /// ID del admin/cashier padre en la jerarquía (generaliza ParentCashierId)
    /// Permite jerarquía multinivel: SUPER_ADMIN ? ADMIN ? SUB_ADMIN ? CASHIER
    /// </summary>
    public Guid? ParentAdminId { get; set; }
    
    /// <summary>
    /// Nivel en la jerarquía: 0=SUPER_ADMIN, 1=BRAND_ADMIN, 2=SUB_ADMIN, 3=CASHIER
    /// </summary>
    public int HierarchyLevel { get; set; } = 0;
    
    /// <summary>
    /// Path jerárquico para queries eficientes (ej: ".root.admin1.subadmin2.")
    /// Permite buscar descendientes con: WHERE hierarchy_path LIKE '.root.admin1.%'
    /// </summary>
    public string? HierarchyPath { get; set; }
    
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
    
    // Hierarchical navigation
    public BackofficeUser? ParentCashier { get; set; }  // DEPRECATED - use ParentAdmin
    public BackofficeUser? ParentAdmin { get; set; }  // NEW: Parent en la jerarquía multinivel
    public ICollection<BackofficeUser> SubordinateCashiers { get; set; } = new List<BackofficeUser>();  // DEPRECATED
    public ICollection<BackofficeUser> SubordinateAdmins { get; set; } = new List<BackofficeUser>();  // NEW: Subordinados directos
    
    public ICollection<CashierPlayer> CashierPlayers { get; set; } = new List<CashierPlayer>();
    public ICollection<BackofficeAudit> BackofficeAudits { get; set; } = new List<BackofficeAudit>();
    
    /// <summary>
    /// Usuario que creó este usuario de backoffice (si aplica)
    /// </summary>
    public BackofficeUser? CreatedByUser { get; set; }
}