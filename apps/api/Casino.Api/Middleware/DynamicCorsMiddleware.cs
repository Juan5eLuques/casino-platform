using Casino.Application.Services;
using System.Text.Json;

namespace Casino.Api.Middleware;

public class DynamicCorsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DynamicCorsMiddleware> _logger;

    public DynamicCorsMiddleware(RequestDelegate next, ILogger<DynamicCorsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, BrandContext brandContext)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        
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
            // Set permissive CORS for these endpoints
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Signature, X-Provider");
            
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

            if (!isAllowed)
            {
                _logger.LogWarning("CORS origin {Origin} not allowed for brand {BrandCode}. Allowed origins: {AllowedOrigins}", 
                    origin, brandContext.BrandCode, string.Join(", ", brandContext.CorsOrigins));
                
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                
                var errorResponse = JsonSerializer.Serialize(new { error = "cors_origin_not_allowed" });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Set CORS headers for allowed origin
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Signature, X-Provider");
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");

            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                return;
            }
        }

        await _next(context);
    }
}