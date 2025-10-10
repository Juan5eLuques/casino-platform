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
        
        // Skip for health checks, swagger, gateway endpoints, and specific auth endpoints
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path.StartsWith("/api/v1/gateway") ||
            path.StartsWith("/api/v1/admin/auth/login") ||  // Login doesn't need brand resolution
            path.StartsWith("/api/v1/admin/auth/logout") || // Logout doesn't need brand resolution
            path.StartsWith("/api/v1/auth/login") ||        // Player login doesn't need brand resolution
            path.StartsWith("/api/v1/auth/logout") ||       // Player logout doesn't need brand resolution
            path == "/" ||
            path.StartsWith("/_"))
        {
            _logger.LogInformation("Skipping brand resolution for path: {Path}", path);
            await _next(context);
            return;
        }

        var host = context.Request.Host.Host.ToLower();
        var port = context.Request.Host.Port;
        var fullHost = context.Request.Host.Value.ToLower();
        
        _logger.LogInformation("Resolving brand for host: {Host}, port: {Port}, fullHost: {FullHost}, path: {Path}", 
            host, port, fullHost, path);

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
                
                // In development, use default brand for localhost scenarios
                if (_env.IsDevelopment() && (host.Contains("localhost") || host.Contains("127.0.0.1")))
                {
                    _logger.LogInformation("Development mode: using default brand for {Host}", host);
                    
                    // Try to get the LOCALHOST_DEV brand first
                    brand = await dbContext.Brands
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Code == "LOCALHOST_DEV" && b.Status == Domain.Enums.BrandStatus.ACTIVE);
                    
                    if (brand == null)
                    {
                        // If LOCALHOST_DEV doesn't exist, try any active brand
                        brand = await dbContext.Brands
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.Status == Domain.Enums.BrandStatus.ACTIVE);
                    }
                    
                    if (brand != null)
                    {
                        _logger.LogInformation("Development mode: using brand {BrandCode} as default for localhost", brand.Code);
                        // Set brand context with the default brand
                        brandContext.BrandId = brand.Id;
                        brandContext.BrandCode = brand.Code;
                        brandContext.Domain = fullHost; // Keep actual host
                        brandContext.CorsOrigins = brand.CorsOrigins ?? new string[0];
                        
                        await _next(context);
                        return;
                    }
                    else
                    {
                        _logger.LogError("Development mode: no active brands found in database");
                    }
                }
                
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                
                var errorResponse = JsonSerializer.Serialize(new { 
                    error = "brand_not_resolved", 
                    host = fullHost,
                    available_brands = await dbContext.Brands
                        .Select(b => new { b.Code, b.Domain, b.AdminDomain })
                        .ToListAsync(),
                    message = "No brand found for this host. Please configure the brand domain in the database."
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