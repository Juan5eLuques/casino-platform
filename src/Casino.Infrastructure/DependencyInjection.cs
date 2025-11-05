using Casino.Application.Abstractions;
using Casino.Infrastructure.Auth;
using Casino.Infrastructure.Clock;
using Casino.Infrastructure.Identity;
using Casino.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Casino.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Default")));

        // JWT
        var key = config["Jwt:Key"]!;
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromMinutes(5), // Tolerancia de 5 minutos para diferencias de reloj
                    RoleClaimType = ClaimTypes.Role // Asegurar que los roles se lean correctamente
                };
                
                o.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                        logger.LogError(ctx.Exception, "JWT Authentication failed: {Message}", ctx.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                        logger.LogInformation("JWT Token validated successfully for user: {User}", 
                            ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown");
                        return Task.CompletedTask;
                    },
                    OnChallenge = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                        logger.LogWarning("JWT Challenge triggered: {Error}", ctx.Error);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IDateTime, SystemDateTime>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}
