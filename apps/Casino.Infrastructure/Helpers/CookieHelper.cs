using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Casino.Infrastructure.Helpers;

/// <summary>
/// Helper centralizado para manejo consistente de cookies de autenticación.
/// Asegura que login y logout usen opciones idénticas.
/// </summary>
public static class CookieHelper
{
    /// <summary>
    /// Obtiene opciones de cookie configuradas de forma consistente para autenticación.
    /// CRÍTICO: Estas opciones deben ser idénticas en login y logout para borrado exitoso.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP actual</param>
    /// <param name="expiresAt">Fecha de expiración opcional (null para cookies de sesión)</param>
    /// <returns>CookieOptions configuradas para escenarios cross-site con proxy</returns>
    public static CookieOptions GetAuthCookieOptions(HttpContext httpContext, DateTimeOffset? expiresAt = null)
    {
        var options = new CookieOptions
        {
            // HttpOnly: Cookie no accesible desde JavaScript (previene XSS)
            HttpOnly = true,
            
            // Secure: Solo enviar por HTTPS (requerido para SameSite=None)
            Secure = true,
            
            // Path: Cookie válida para todas las rutas
            Path = "/",
            
            // SameSite=None: CRÍTICO para funcionamiento con proxies cross-site
            // (Frontend en Netlify ? Backend en Railway)
            // Sin esto, los navegadores modernos bloquean la cookie en requests cross-site
            SameSite = SameSiteMode.None,
            
            // Domain: NO SE SETEA INTENCIONALMENTE
            // Dejar implícito crea "host-only cookie" que funciona mejor con proxies
            // Setting Domain would cause issues in proxy scenarios
            
            // Expires: Solo si se proporciona
            Expires = expiresAt
        };
        
        return options;
    }
    
    /// <summary>
    /// Obtiene el nombre de cookie para backoffice específico del brand.
    /// Permite múltiples sesiones simultáneas en diferentes brands.
    /// </summary>
    /// <param name="brandCode">Código del brand (ej: "NETLIFY_PROD")</param>
    /// <returns>Nombre de cookie único por brand (ej: "bk.token.netlify_prod")</returns>
    public static string GetBackofficeCookieName(string brandCode)
    {
        if (string.IsNullOrWhiteSpace(brandCode))
        {
            throw new ArgumentException("Brand code cannot be null or empty", nameof(brandCode));
        }
        
        return $"bk.token.{brandCode.ToLower()}";
    }
    
    /// <summary>
    /// Obtiene el nombre de cookie para players.
    /// </summary>
    /// <returns>Nombre de cookie estándar para players: "pl.token"</returns>
    public static string GetPlayerCookieName()
    {
        return "pl.token";
    }
    
    /// <summary>
    /// Elimina una cookie de autenticación usando doble estrategia para máxima compatibilidad.
    /// Estrategia 1: Delete (marca cookie para borrado)
    /// Estrategia 2: Append con valor vacío y fecha pasada (fuerza expiración)
    /// </summary>
    /// <param name="httpContext">Contexto HTTP actual</param>
    /// <param name="cookieName">Nombre de la cookie a eliminar</param>
    /// <param name="logger">Logger para auditoría</param>
    public static void DeleteAuthCookie(HttpContext httpContext, string cookieName, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(cookieName))
        {
            throw new ArgumentException("Cookie name cannot be null or empty", nameof(cookieName));
        }
        
        // Verificar si la cookie está presente en el request
        var cookieWasPresent = httpContext.Request.Cookies.ContainsKey(cookieName);
        
        logger.LogInformation(
            "Deleting auth cookie: {CookieName}, Present in request: {Present}",
            cookieName, cookieWasPresent);
        
        // Obtener opciones idénticas a las del login
        var cookieOptions = GetAuthCookieOptions(httpContext, expiresAt: null);
        
        // ESTRATEGIA 1: Delete
        // Marca la cookie para borrado en el navegador
        httpContext.Response.Cookies.Delete(cookieName, cookieOptions);
        
        // ESTRATEGIA 2: Append con valor vacío y fecha pasada
        // Fuerza expiración inmediata (fallback si Delete no funciona)
        cookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(-1);
        httpContext.Response.Cookies.Append(cookieName, string.Empty, cookieOptions);
        
        logger.LogInformation(
            "Auth cookie deleted using dual strategy: {CookieName}",
            cookieName);
    }
    
    /// <summary>
    /// Valida si una cookie está presente en el request.
    /// Útil para logging y debugging.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP actual</param>
    /// <param name="cookieName">Nombre de la cookie a verificar</param>
    /// <returns>True si la cookie está presente</returns>
    public static bool IsCookiePresent(HttpContext httpContext, string cookieName)
    {
        return httpContext.Request.Cookies.ContainsKey(cookieName);
    }
}
