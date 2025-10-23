using System.Text.Json;

namespace Casino.Domain.Entities;

/// <summary>
/// Representa un juego disponible en la plataforma
/// Extendido con campos de catálogo para agregador multi-proveedor
/// </summary>
public class Game
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
  public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
  
    // ? NEW FIELDS FOR CATALOG AND LAUNCH
    
    /// <summary>
    /// Referencia al proveedor (relación con GameProviders)
    /// </summary>
  public Guid? ProviderId { get; set; }
    
    /// <summary>
    /// ID del juego en el sistema del proveedor (ej: "vs20sbxmas" para Pragmatic)
    /// Si es null, se usa Code como launchId
    /// </summary>
 public string? LaunchId { get; set; }
  
    /// <summary>
    /// Tipo de juego: SLOT, LIVE_CASINO, TABLE, CRASH, OTHER
    /// </summary>
    public Enums.GameType Type { get; set; } = Enums.GameType.SLOT;
    
    /// <summary>
    /// Return to Player percentage (ej: 96.51)
/// </summary>
    public decimal? RTP { get; set; }
 
    /// <summary>
    /// Volatilidad del juego: "LOW", "MEDIUM", "HIGH"
    /// </summary>
    public string? Volatility { get; set; }
  
    /// <summary>
    /// Categoría del juego: "slots", "table", "live", "crash", etc.
  /// </summary>
public string? Category { get; set; }
    
    /// <summary>
    /// URL de la imagen/thumbnail del juego
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// Apuesta mínima permitida
    /// </summary>
    public decimal? MinBet { get; set; }
    
    /// <summary>
  /// Apuesta máxima permitida
    /// </summary>
    public decimal? MaxBet { get; set; }
    
    /// <summary>
    /// Indica si el juego es destacado/featured
    /// </summary>
    public bool IsFeatured { get; set; } = false;
    
    /// <summary>
    /// Indica si el juego es nuevo (badge "New")
    /// </summary>
    public bool IsNew { get; set; } = false;
    
    /// <summary>
    /// Tags adicionales para búsquedas y filtros
    /// </summary>
    public string[] AdditionalTags { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Fecha de última actualización de metadata
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public GameProvider? ProviderEntity { get; set; }
 public ICollection<BrandGame> BrandGames { get; set; } = new List<BrandGame>();
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
    public ICollection<GameLaunchLog> LaunchLogs { get; set; } = new List<GameLaunchLog>();
}