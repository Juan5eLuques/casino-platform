using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

public class Round
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public RoundStatus Status { get; set; }
    public long TotalBetBigint { get; set; }
    public long TotalWinBigint { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    // Navigation properties
    public GameSession Session { get; set; } = null!;
    public ICollection<Ledger> LedgerEntries { get; set; } = new List<Ledger>();
}