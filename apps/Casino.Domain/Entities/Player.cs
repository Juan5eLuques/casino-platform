using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string? ExternalId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public PlayerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public Wallet? Wallet { get; set; }
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    public ICollection<Ledger> LedgerEntries { get; set; } = new List<Ledger>();
    public ICollection<CashierPlayer> CashierPlayers { get; set; } = new List<CashierPlayer>();
}