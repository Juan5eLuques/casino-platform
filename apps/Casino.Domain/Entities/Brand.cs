using Casino.Domain.Enums;
using System.Text.Json;

namespace Casino.Domain.Entities;

public class Brand
{
    public Guid Id { get; set; }
    public Guid OperatorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public JsonDocument? Theme { get; set; }
    public BrandStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Operator Operator { get; set; } = null!;
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<BrandGame> BrandGames { get; set; } = new List<BrandGame>();
    public ICollection<Ledger> LedgerEntries { get; set; } = new List<Ledger>();
}