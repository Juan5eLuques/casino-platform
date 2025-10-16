using Casino.Application.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Casino.Api.Middleware;

public class DynamicCorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DynamicCorsMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public DynamicCorsMiddleware(RequestDelegate next, ILogger<DynamicCorsMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, BrandContext brandContext, Infrastructure.Data.CasinoDbContext dbContext)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var method = context.Request.Method;
        
        _logger.LogInformation("CORS Request: {Method} {Path} from Origin: {Origin}", 
            method, context.Request.Path, origin ?? "none");
        
        // Skip CORS for same-origin requests or when no origin is present
        if (string.IsNullOrEmpty(origin))
        {
            await _next(context);
            return;
        }

        // Skip CORS validation for certain paths
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path == "/" ||
            path.StartsWith("/_"))
        {
            // Set CORS headers for these endpoints - always use specific origin, never wildcard
            SetCorsHeaders(context, origin);
            
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                return;
            }
            
            await _next(context);
            return;
        }

        // For auth endpoints that don't require brand resolution
        if (path.StartsWith("/api/v1/admin/auth") || path.StartsWith("/api/v1/auth"))
        {
            _logger.LogInformation("CORS validation for auth endpoint: {Path} from {Origin}", path, origin);
            
            // Check if origin is allowed (development origins or any origin in database)
            var isDevOriginAllowed = IsOriginAllowedForAuth(origin) || 
                (_env.IsDevelopment() && IsOriginAllowedForDevelopment(origin));
            
            if (!isDevOriginAllowed)
            {
                // Check if origin exists in ANY brand's CORS origins in database
                var brands = await dbContext.Brands
                    .AsNoTracking()
                    .Where(b => b.Status == Domain.Enums.BrandStatus.ACTIVE)
                    .ToListAsync();
                
                var isOriginInDatabase = brands.Any(b => 
                    b.CorsOrigins != null && 
                    b.CorsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase));
                
                if (!isOriginInDatabase)
                {
                    _logger.LogWarning("CORS origin {Origin} not allowed for auth endpoint (not in development list and not in any active brand)", origin);
                    
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = JsonSerializer.Serialize(new { 
                        error = "cors_origin_not_allowed", 
                        origin = origin,
                        endpoint = "auth",
                        message = "Origin not found in any active brand's CORS origins"
                    });
                    await context.Response.WriteAsync(errorResponse);
                    return;
                }
                
                _logger.LogInformation("CORS origin {Origin} allowed for auth endpoint (found in database)", origin);
            }

            SetCorsHeaders(context, origin);

            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                return;
            }

            await _next(context);
            return;
        }

        // For brand-resolved requests, check CORS origins
        if (brandContext.IsResolved)
        {
            var isAllowed = brandContext.CorsOrigins.Length == 0 || // If no CORS origins configured, allow all
                           brandContext.CorsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
            
            // SONNET: Fallback para orígenes conocidos (development y production)
            if (!isAllowed)
            {
                isAllowed = IsOriginAllowedForAuth(origin) || 
                           (_env.IsDevelopment() && IsOriginAllowedForDevelopment(origin));
                if (isAllowed)
                {
                    _logger.LogInformation("CORS allowed for origin {Origin} via fallback (known origin)", origin);
                }
            }

            if (!isAllowed)
            {
                _logger.LogWarning("CORS origin {Origin} not allowed for brand {BrandCode}. Allowed origins: {AllowedOrigins}", 
                    origin, brandContext.BrandCode, string.Join(", ", brandContext.CorsOrigins));
                
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                
                var errorResponse = JsonSerializer.Serialize(new { 
                    error = "cors_origin_not_allowed", 
                    origin = origin,
                    brand = brandContext.BrandCode,
                    allowedOrigins = brandContext.CorsOrigins 
                });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            _logger.LogInformation("CORS allowed for origin {Origin} on brand {BrandCode}", origin, brandContext.BrandCode);

            // Set CORS headers for allowed origin
            SetCorsHeaders(context, origin);

            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                return;
            }

            await _next(context);
        }
        else
        {
            // Brand not resolved - this should now be rare since BrandResolver runs first
            _logger.LogError("Brand not resolved for CORS request to {Path} from {Origin}. " +
                "This indicates BrandResolverMiddleware failed or skipped resolution.", path, origin);
            
            // In development, be more permissive for certain origins
            if (_env.IsDevelopment() && (IsOriginAllowedForAuth(origin) || IsOriginAllowedForDevelopment(origin)))
            {
                _logger.LogWarning("Development mode: allowing unresolved brand request from {Origin}", origin);
                SetCorsHeaders(context, origin);
                
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    return;
                }
                
                await _next(context);
                return;
            }
            
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            
            var errorResponse = JsonSerializer.Serialize(new { 
                error = "brand_not_resolved",
                message = "Brand context could not be resolved for this request",
                host = context.Request.Host.Host,
                path = path
            });
            await context.Response.WriteAsync(errorResponse);
        }
    }

    private bool IsOriginAllowedForAuth(string origin)
    {
        // SONNET: Development origins are always allowed for auth endpoints
        var developmentOrigins = new[]
        {
            "http://localhost:5173",
            "http://localhost:3000",
            "http://localhost:5000",
            "http://127.0.0.1:5173",
            "http://admin.bet30.local:5173",
            "https://admin.bet30.local:5173",
            "http://bet30.local:5173",
            "https://bet30.local:5173"
        };
        
        return developmentOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private bool IsOriginAllowedForDevelopment(string origin)
    {
        // Additional permissive origins for development
        var devOrigins = new[]
        {
            "http://localhost:7182",
            "https://localhost:7182",
            "http://127.0.0.1:7182",
            "https://127.0.0.1:7182",
            "http://localhost:4200",
            "http://localhost:8080"
        };
        
        return devOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private void SetCorsHeaders(HttpContext context, string origin)
    {
        // NEVER use wildcard (*) when credentials are included
        // Always use the specific origin
        context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Signature, X-Provider, X-Requested-With");
        context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
        context.Response.Headers.Append("Access-Control-Max-Age", "86400"); // 24 hours
        // SONNET: Exponer Set-Cookie para que el navegador pueda leer las cookies del backend
        context.Response.Headers.Append("Access-Control-Expose-Headers", "Set-Cookie");
        
        _logger.LogDebug("CORS headers set for origin: {Origin}", origin);
    }
}