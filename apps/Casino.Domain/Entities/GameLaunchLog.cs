namespace Casino.Domain.Entities;

/// <summary>
/// Log de auditoría de cada lanzamiento de juego
/// Registra la URL generada, el token y el resultado del launch
/// </summary>
public class GameLaunchLog
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Sesión de juego asociada
    /// </summary>
public Guid SessionId { get; set; }
    
  /// <summary>
    /// Jugador que lanzó el juego
    /// </summary>
    public Guid PlayerId { get; set; }
  
 /// <summary>
    /// Juego que fue lanzado
    /// </summary>
public Guid GameId { get; set; }
    
    /// <summary>
    /// Brand desde donde se lanzó el juego
    /// </summary>
    public Guid BrandId { get; set; }
    
  /// <summary>
    /// Código del proveedor (para queries rápidas)
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// URL completa generada para el iframe
    /// </summary>
    public string LaunchUrl { get; set; } = string.Empty;
  
/// <summary>
    /// Token de sesión generado para el proveedor (puede estar encriptado)
 /// </summary>
    public string SessionToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si el launch fue exitoso
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Mensaje de error si el launch falló
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// IP del jugador que hizo el launch
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User-Agent del navegador
    /// </summary>
    public string? UserAgent { get; set; }
  
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public GameSession Session { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Game Game { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
}
