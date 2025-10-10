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
    
    /// <summary>
    /// ID del usuario que creó este jugador (backoffice user)
    /// </summary>
    public Guid? CreatedByUserId { get; set; }
    
    // SONNET: Rol del usuario que creó este jugador (para auditoría)
    /// <summary>
    /// Rol del usuario que creó este jugador
    /// </summary>
    public string? CreatedByRole { get; set; }
    
    /// <summary>
    /// Balance del wallet en formato decimal (paralelo al sistema bigint existente)
    /// </summary>
    public decimal WalletBalance { get; set; } = 0.00m;
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public Wallet? Wallet { get; set; }
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    public ICollection<Ledger> LedgerEntries { get; set; } = new List<Ledger>();
    public ICollection<CashierPlayer> CashierPlayers { get; set; } = new List<CashierPlayer>();
    
    /// <summary>
    /// Usuario de backoffice que creó este jugador (si aplica)
    /// </summary>
    public BackofficeUser? CreatedByUser { get; set; }
}