using System.Text.Json;

namespace Casino.Application.Providers;

/// <summary>
/// Interfaz base para adapters de proveedores de juegos
/// Cada proveedor debe implementar su propio adapter
/// </summary>
public interface IProviderAdapter
{
  /// <summary>
    /// Código único del proveedor (debe coincidir con GameProvider.Code)
    /// </summary>
    string ProviderCode { get; }
    
    /// <summary>
    /// Lanza un juego y retorna la URL del iframe
    /// </summary>
    Task<GameLaunchResponse> LaunchGameAsync(
    LaunchGameRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Valida si un token de sesión es válido (opcional)
    /// </summary>
    Task<bool> ValidateSessionAsync(
        string sessionToken, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request para lanzar un juego
/// </summary>
public record LaunchGameRequest(
    string GameCode,
    string LaunchId,
    Guid PlayerId,
    string PlayerUsername,
    decimal PlayerBalance,
    string BrandSecret,
    bool IsDemo,
    string? ReturnUrl,
 JsonDocument? ProviderMeta
);

/// <summary>
/// Response del lanzamiento de juego
/// </summary>
public record GameLaunchResponse(
    bool Success,
    string? LaunchUrl,
    string? SessionToken,
    DateTime? ExpiresAt,
    string? ErrorMessage
);
