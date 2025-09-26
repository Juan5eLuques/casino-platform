using Casino.Domain.Enums;

namespace Casino.Domain.Entities;

public class GameSession
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string GameCode { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public GameSessionStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Player Player { get; set; } = null!;
    public ICollection<Round> Rounds { get; set; } = new List<Round>();
}