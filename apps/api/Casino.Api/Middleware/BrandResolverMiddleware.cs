using Casino.Application.Services;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Casino.Api.Middleware;

public class BrandResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BrandResolverMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public BrandResolverMiddleware(RequestDelegate next, ILogger<BrandResolverMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, CasinoDbContext dbContext, BrandContext brandContext)
    {
        // Skip brand resolution for certain paths that don't need it
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        
        // Skip ONLY for health checks, swagger, gateway endpoints, and logout
        // LOGIN REQUIRES brand resolution to validate user belongs to brand
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path.StartsWith("/api/v1/gateway") ||
            path.StartsWith("/api/v1/admin/auth/logout") || // Logout doesn't need brand resolution
            path.StartsWith("/api/v1/auth/logout") ||       // Player logout doesn't need brand resolution
            path == "/" ||
            path.StartsWith("/_"))
        {
            _logger.LogInformation("Skipping brand resolution for path: {Path}", path);
            await _next(context);
            return;
        }

        // SONNET: Priorizar Origin header (dominio del frontend) sobre Host (dominio del backend)
        var originHeader = context.Request.Headers["Origin"].FirstOrDefault();
        var refererHeader = context.Request.Headers["Referer"].FirstOrDefault();
        
        // Intentar extraer el dominio del Origin o Referer
        string? resolvedHost = null;
        if (!string.IsNullOrEmpty(originHeader))
        {
            // Origin: https://backoffice-casino.netlify.app
            resolvedHost = new Uri(originHeader).Host.ToLower();
            _logger.LogInformation("Resolving brand by Origin header: {Origin} -> {ResolvedHost}", originHeader, resolvedHost);
        }
        else if (!string.IsNullOrEmpty(refererHeader))
        {
            // Referer: https://backoffice-casino.netlify.app/some-page
            resolvedHost = new Uri(refererHeader).Host.ToLower();
            _logger.LogInformation("Resolving brand by Referer header: {Referer} -> {ResolvedHost}", refererHeader, resolvedHost);
        }
        else
        {
            // Fallback: usar el Host del backend (para requests directas a la API)
            resolvedHost = context.Request.Host.Host.ToLower();
            _logger.LogInformation("Resolving brand by Host (fallback): {Host}", resolvedHost);
        }
        
        var host = resolvedHost;
        var port = context.Request.Host.Port;
        var fullHost = resolvedHost;
        
        _logger.LogInformation("Final resolved host for brand lookup: {Host}, path: {Path}", host, path);

        try
        {
            // Look for brand by domain or admin_domain - try both full host and just hostname
            var brand = await dbContext.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(b => 
                    (b.Domain != null && (b.Domain.ToLower() == fullHost || b.Domain.ToLower() == host)) ||
                    (b.AdminDomain != null && (b.AdminDomain.ToLower() == fullHost || b.AdminDomain.ToLower() == host)));

            if (brand == null)
            {
                var availableBrands = await dbContext.Brands
                    .Select(b => $"{b.Code}:Domain={b.Domain}:AdminDomain={b.AdminDomain}")
                    .ToListAsync();
                    
                _logger.LogWarning("Brand not resolved for host: {Host} (full: {FullHost}). Available brands: {AvailableBrands}", 
                    host, fullHost, string.Join(", ", availableBrands));
                
                // CRITICAL: In development with localhost, do NOT use a default brand
                // This would bypass brand validation in login
                // Instead, return error to force proper brand configuration
                if (_env.IsDevelopment() && (host.Contains("localhost") || host.Contains("127.0.0.1")))
                {
                    _logger.LogWarning(
                        "Development mode: localhost detected but NO default brand will be used. " +
                        "Configure /etc/hosts with brand domains (e.g., sitea.local, siteb.local) or use production domains.");
                }
                
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                
                var errorResponse = JsonSerializer.Serialize(new { 
                    error = "brand_not_resolved", 
                    host = fullHost,
                    available_brands = await dbContext.Brands
                        .Select(b => new { b.Code, b.Domain, b.AdminDomain })
                        .ToListAsync(),
                    message = "No brand found for this host. Please configure the brand domain in the database or use a configured domain.",
                    hint_localhost = _env.IsDevelopment() 
                        ? "For localhost development, configure /etc/hosts (Linux/Mac) or C:\\Windows\\System32\\drivers\\etc\\hosts (Windows) with brand domains like '127.0.0.1 sitea.local'"
                        : null
                });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Check if brand is active
            if (brand.Status != Domain.Enums.BrandStatus.ACTIVE)
            {
                _logger.LogWarning("Brand {BrandCode} is not active for host: {Host}", brand.Code, fullHost);
                
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                
                var errorResponse = JsonSerializer.Serialize(new { 
                    error = "brand_not_active",
                    brand = brand.Code
                });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Set brand context
            brandContext.BrandId = brand.Id;
            brandContext.BrandCode = brand.Code;
            brandContext.Domain = fullHost;
            brandContext.CorsOrigins = brand.CorsOrigins ?? new string[0];

            _logger.LogInformation("Brand resolved: {BrandCode} ({BrandId}) for host: {FullHost}, CORS origins: {CorsOrigins}", 
                brand.Code, brand.Id, fullHost, string.Join(", ", brandContext.CorsOrigins));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during brand resolution for host: {Host}", host);
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            var errorResponse = JsonSerializer.Serialize(new { 
                error = "brand_resolution_error",
                message = "An error occurred while resolving the brand"
            });
            await context.Response.WriteAsync(errorResponse);
            return;
        }

        // Brand resolved successfully, continue to next middleware
        await _next(context);
    }
}