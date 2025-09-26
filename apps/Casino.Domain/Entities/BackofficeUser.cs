using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

public class BackofficeUser
{
    public Guid Id { get; set; }
    public Guid? OperatorId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public BackofficeUserRole Role { get; set; }
    public BackofficeUserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public Operator? Operator { get; set; }
    public ICollection<CashierPlayer> CashierPlayers { get; set; } = new List<CashierPlayer>();
    public ICollection<BackofficeAudit> BackofficeAudits { get; set; } = new List<BackofficeAudit>();
}