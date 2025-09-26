namespace Casino.Domain.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<BrandGame> BrandGames { get; set; } = new List<BrandGame>();
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
}