using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

public class Operator
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public OperatorStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Brand> Brands { get; set; } = new List<Brand>();
    public ICollection<BackofficeUser> BackofficeUsers { get; set; } = new List<BackofficeUser>();
    public ICollection<Ledger> LedgerEntries { get; set; } = new List<Ledger>();
}