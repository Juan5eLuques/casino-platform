using Casino.Application.Services;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Casino.Api.Middleware;

public class BrandResolverMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BrandResolverMiddleware> _logger;

    public BrandResolverMiddleware(RequestDelegate next, ILogger<BrandResolverMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CasinoDbContext dbContext, BrandContext brandContext)
    {
        // Skip brand resolution for certain paths that don't need it
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        
        // Skip for health checks, swagger, and gateway endpoints (they resolve brand differently)
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path.StartsWith("/api/v1/gateway") ||
            path == "/" ||
            path.StartsWith("/_"))
        {
            await _next(context);
            return;
        }

        var host = context.Request.Host.Host.ToLower();
        
        _logger.LogInformation("Resolving brand for host: {Host}", host);

        try
        {
            // Look for brand by domain or admin_domain
            var brand = await dbContext.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(b => 
                    (b.Domain != null && b.Domain.ToLower() == host) ||
                    (b.AdminDomain != null && b.AdminDomain.ToLower() == host));

            if (brand == null)
            {
                _logger.LogWarning("Brand not resolved for host: {Host}", host);
                
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                
                var errorResponse = JsonSerializer.Serialize(new { error = "brand_not_resolved" });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Check if brand is active
            if (brand.Status != Domain.Enums.BrandStatus.ACTIVE)
            {
                _logger.LogWarning("Brand {BrandCode} is not active for host: {Host}", brand.Code, host);
                
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                
                var errorResponse = JsonSerializer.Serialize(new { error = "brand_not_active" });
                await context.Response.WriteAsync(errorResponse);
                return;
            }

            // Set brand context
            brandContext.BrandId = brand.Id;
            brandContext.BrandCode = brand.Code;
            brandContext.Domain = host;
            brandContext.CorsOrigins = brand.CorsOrigins;
            brandContext.OperatorId = brand.OperatorId;

            _logger.LogInformation("Brand resolved: {BrandCode} ({BrandId}) for host: {Host}", 
                brand.Code, brand.Id, host);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving brand for host: {Host}", host);
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            var errorResponse = JsonSerializer.Serialize(new { error = "internal_server_error" });
            await context.Response.WriteAsync(errorResponse);
        }
    }
}