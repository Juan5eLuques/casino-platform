using System.Text.Json;

namespace Casino.Domain.Entities;

/// <summary>
/// Representa un proveedor de juegos externo (Pragmatic, Evolution, etc.)
/// Almacena la configuración genérica para lanzar juegos de cada proveedor
/// </summary>
public class GameProvider
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Código único del proveedor (ej: "pragmatic", "evolution", "mock")
  /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre legible del proveedor (ej: "Pragmatic Play")
    /// </summary>
    public string Name { get; set; } = string.Empty;
  
    /// <summary>
    /// Plantilla del endpoint de lanzamiento con placeholders
    /// Ejemplo: "https://api.pragmatic.com/launch?token={token}&game={gameSymbol}"
    /// </summary>
    public string LaunchEndpointTemplate { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si el proveedor requiere un token de sesión firmado
 /// </summary>
    public bool RequiresSessionToken { get; set; } = true;
    
    /// <summary>
    /// Indica si el proveedor soporta juego con dinero real
    /// </summary>
    public bool SupportsRealMode { get; set; } = true;
    
    /// <summary>
    /// Indica si el proveedor soporta modo demo/fun
    /// </summary>
    public bool SupportsDemoMode { get; set; } = false;
    
 /// <summary>
    /// Metadata adicional del proveedor (configuraciones específicas en JSON)
    /// </summary>
    public JsonDocument? DefaultMeta { get; set; }
    
    /// <summary>
    /// Estado del proveedor (activo/inactivo a nivel global)
  /// </summary>
    public bool Enabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Game> Games { get; set; } = new List<Game>();
}
