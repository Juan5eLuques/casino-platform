using Casino.Application.DTOs.Auth;
using Casino.Application.Services;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Admin authentication endpoints (unprotected)
        app.MapPost("/api/v1/admin/auth/login", AdminLogin)
            .WithName("AdminLogin")
            .WithTags("Auth");

        app.MapPost("/api/v1/admin/auth/logout", AdminLogout)
            .RequireAuthorization("BackofficePolicy")
            .WithName("AdminLogout")
            .WithTags("Auth");

        app.MapGet("/api/v1/admin/auth/me", GetAdminProfile)
            .RequireAuthorization("BackofficePolicy")
            .WithName("GetAdminProfile")
            .WithTags("Auth");

        // Player authentication endpoints (unprotected)
        app.MapPost("/api/v1/auth/login", PlayerLogin)
            .WithName("PlayerLogin")
            .WithTags("Auth");

        app.MapPost("/api/v1/auth/logout", PlayerLogout)
            .RequireAuthorization("PlayerPolicy")
            .WithName("PlayerLogout")
            .WithTags("Auth");

        app.MapGet("/api/v1/auth/me", GetPlayerProfile)
            .RequireAuthorization("PlayerPolicy")
            .WithName("GetPlayerProfile")
            .WithTags("Auth");
    }

    public static async Task<IResult> AdminLogin(
        [FromBody] AdminLoginRequest request,
        CasinoDbContext db,
        BrandContext brandContext,
        IJwtService jwtService,
        IPasswordService passwordService,
        HttpContext httpContext,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        
        try
        {
            // Validate configuration first
            var jwtKey = configuration["Auth:JwtKey"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                logger.LogError("JWT configuration missing: Auth:JwtKey is not configured");
                return Results.Problem(
                    title: "Configuration Error",
                    detail: "JwtKey missing - server configuration error",
                    statusCode: 500);
            }

            logger.LogInformation("Admin login attempt for username: {Username} on host: {Host}", 
                request.Username, httpContext.Request.Host.Host);

            // Validate input
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                logger.LogWarning("Admin login attempt with empty credentials");
                return Results.Unauthorized();
            }

            // CRITICAL: Validate brand context is resolved
            if (!brandContext.IsResolved)
            {
                logger.LogWarning("Admin login attempted without resolved brand context on host: {Host}", 
                    httpContext.Request.Host.Host);
                return Results.Problem(
                    title: "Brand Not Resolved",
                    detail: "Cannot login without a valid brand context. Ensure you're accessing from a configured domain.",
                    statusCode: 400);
            }

            // Find user
            var user = await db.BackofficeUsers
                .Include(u => u.Brand)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Status == BackofficeUserStatus.ACTIVE);

            if (user == null)
            {
                logger.LogWarning("Admin login failed: user not found or inactive for username: {Username}", request.Username);
                return Results.Unauthorized();
            }

            // CRITICAL: Validate user belongs to the current brand
            // Only SUPER_ADMIN can login from any brand
            if (user.Role != BackofficeUserRole.SUPER_ADMIN)
            {
                if (!user.BrandId.HasValue || user.BrandId.Value != brandContext.BrandId)
                {
                    logger.LogWarning(
                        "Admin login failed: user {Username} (BrandId: {UserBrandId}) attempted login on different brand {CurrentBrandId} ({CurrentBrandCode})",
                        request.Username, user.BrandId, brandContext.BrandId, brandContext.BrandCode);
                    return Results.Problem(
                        title: "Brand Mismatch",
                        detail: "This user account is not authorized for this brand/site.",
                        statusCode: 403);
                }
            }

            // Check password hash
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                logger.LogWarning("Admin login failed: user {Username} has no password hash", request.Username);
                return Results.Unauthorized();
            }

            // Verify password - CORRECTED: order of parameters (password, hash)
            if (!passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                logger.LogWarning("Admin login failed: invalid password for username: {Username}", request.Username);
                return Results.Unauthorized();
            }

            // Create claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString()),
                // CRITICAL: Always include current brand in token
                new("brand_id", brandContext.BrandId.ToString()),
                new("brand_code", brandContext.BrandCode)
            };

            // Issue JWT con aud = "backoffice" y claims de rol + brand
            var tokenResponse = jwtService.IssueToken("backoffice", claims, TimeSpan.FromHours(8));

            // CRITICAL: Set cookie with brand-specific NAME to allow multiple sessions
            // Use brand code in cookie name so each brand has independent cookies
            var cookieName = $"bk.token.{brandContext.BrandCode.ToLower()}";
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,                 // Cookie no accesible desde JavaScript (seguridad)
                Secure = true,                   // HTTPS obligatorio en producción
                Path = "/",                      // Path "/" para cubrir todas las rutas /api/*
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            };

            // CRITICAL: Configure SameSite based on environment
            var host = httpContext.Request.Host.Host;
            var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();
            
            // Check if frontend is on different domain (cross-site scenario)
            bool isCrossSite = !string.IsNullOrEmpty(origin) && 
                              !origin.Contains(host) && 
                              !host.Contains("localhost") && 
                              !host.StartsWith("127.0.0.1");

            if (isCrossSite)
            {
                // Cross-site scenario (e.g., netlify.app → railway.app)
                // MUST use SameSite=None to allow cross-site cookies
                cookieOptions.SameSite = SameSiteMode.None;
                logger.LogInformation("Cross-site detected: Origin={Origin}, Host={Host} → Using SameSite=None", 
                    origin, host);
            }
            else
            {
                // Same-site or localhost → use Lax for better security
                cookieOptions.SameSite = SameSiteMode.Lax;
                logger.LogInformation("Same-site or localhost detected → Using SameSite=Lax");
            }

            // OPTIONAL: Set Domain for production multi-brand isolation
            // Only set if not localhost (development)
            if (!host.Contains("localhost") && !host.StartsWith("127.0.0.1"))
            {
                // Set cookie domain to current host for isolation
                cookieOptions.Domain = host;
                logger.LogInformation("Setting cookie domain to: {Domain}", host);
            }
            
            // Use brand-specific cookie name
            httpContext.Response.Cookies.Append(cookieName, tokenResponse.AccessToken, cookieOptions);
            
            logger.LogInformation("Cookie set: {CookieName} for brand {BrandCode}", cookieName, brandContext.BrandCode);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation(
                "✅ Successful admin login - User: {UserId} ({Username}) - Role: {Role} - Brand: {BrandCode} ({BrandId})", 
                user.Id, user.Username, user.Role, brandContext.BrandCode, brandContext.BrandId);

            // Return success response with brand info
            return Results.Ok(new { 
                ok = true, 
                user = new { 
                    user.Id, 
                    user.Username, 
                    Role = user.Role.ToString() 
                },
                brand = new {
                    brandContext.BrandId,
                    brandContext.BrandCode,
                    brandContext.Domain
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error during admin login for username: {Username}", request.Username);
            return Results.Problem(
                title: "Login Error",
                detail: "An internal error occurred during login",
                statusCode: 500);
        }
    }

    public static async Task<IResult> PlayerLogin(
        [FromBody] PlayerLoginRequest request,
        CasinoDbContext db,
        BrandContext brandContext,
        IJwtService jwtService,
        IPasswordService passwordService,
        HttpContext httpContext,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        
        try
        {
            // Validate configuration first
            var jwtKey = configuration["Auth:JwtKey"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                logger.LogError("JWT configuration missing: Auth:JwtKey is not configured");
                return Results.Problem(
                    title: "Configuration Error", 
                    detail: "JwtKey missing - server configuration error",
                    statusCode: 500);
            }

            // Validate brand context
            if (!brandContext.IsResolved)
            {
                logger.LogWarning("Player login attempted without resolved brand context");
                return Results.Problem(
                    title: "Brand Not Resolved",
                    detail: "Brand context is not available",
                    statusCode: 400);
            }

            logger.LogInformation("Player login attempt for brand: {BrandCode}", brandContext.BrandCode);

            // Validate input
            if (string.IsNullOrEmpty(request.Password) || 
                (string.IsNullOrEmpty(request.Username) && !request.PlayerId.HasValue))
            {
                logger.LogWarning("Player login attempt with invalid credentials for brand: {BrandCode}", brandContext.BrandCode);
                return Results.Unauthorized();
            }

            // Find player
            var query = db.Players
                .Include(p => p.Brand)
                .Include(p => p.Wallet)
                .Where(p => p.BrandId == brandContext.BrandId && p.Status == PlayerStatus.ACTIVE);

            var player = request.PlayerId.HasValue
                ? await query.FirstOrDefaultAsync(p => p.Id == request.PlayerId)
                : await query.FirstOrDefaultAsync(p => p.Username == request.Username);

            if (player == null)
            {
                logger.LogWarning("Player login failed: player not found or inactive for brand: {BrandCode}", brandContext.BrandCode);
                return Results.Unauthorized();
            }

            // For demo purposes, we're not requiring password hash for players yet
            // In production, implement proper password validation like admin
            // TODO: Implement password hashing for players when player registration is implemented
            
            // For now, accept any password for demo players (this is for development only)
            logger.LogInformation("Player login (demo mode): {PlayerId} - {Username} for brand: {BrandCode}", 
                player.Id, player.Username, brandContext.BrandCode);

            // Create claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, player.Id.ToString()),
                new(ClaimTypes.Name, player.Username),
                new(ClaimTypes.Role, "PLAYER"),
                new("brand_id", brandContext.BrandId.ToString()),
                new("brand_code", brandContext.BrandCode)
            };

            // Issue JWT
            var tokenResponse = jwtService.IssueToken("player", claims, TimeSpan.FromHours(8));

            // Set HttpOnly cookie
            httpContext.Response.Cookies.Append(
                "pl.token",
                tokenResponse.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Secure only if HTTPS
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = tokenResponse.ExpiresAt
                });

            logger.LogInformation("Successful player login: {PlayerId} - {Username} for brand: {BrandCode}", 
                player.Id, player.Username, brandContext.BrandCode);

            var playerResponse = new
            {
                player.Id,
                player.Username,
                Brand = new { brandContext.BrandId, brandContext.BrandCode },
                Balance = player.Wallet?.BalanceBigint ?? 0
            };

            return TypedResults.Ok(new LoginResponse(
                Success: true,
                User: playerResponse,
                ExpiresAt: tokenResponse.ExpiresAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error during player login for brand: {BrandCode}", 
                brandContext.BrandCode ?? "unknown");
            return Results.Problem(
                title: "Login Error",
                detail: "An internal error occurred during login",
                statusCode: 500);
        }
    }

    public static IResult AdminLogout(HttpContext httpContext, BrandContext brandContext)
    {
        // CRITICAL: Use brand-specific cookie name (same as login)
        var cookieName = $"bk.token.{brandContext.BrandCode.ToLower()}";
        
        // CRITICAL: Match cookie options with login for proper deletion
        var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();
        var host = httpContext.Request.Host.Host;
        
        bool isCrossSite = !string.IsNullOrEmpty(origin) && 
                          !origin.Contains(host) && 
                          !host.Contains("localhost") && 
                          !host.StartsWith("127.0.0.1");

        var cookieOptions = new CookieOptions 
        { 
            Path = "/",
            Secure = true,
            HttpOnly = true,
            SameSite = isCrossSite ? SameSiteMode.None : SameSiteMode.Lax
        };

        // Set domain if not localhost
        if (!host.Contains("localhost") && !host.StartsWith("127.0.0.1"))
        {
            cookieOptions.Domain = host;
        }
        
        httpContext.Response.Cookies.Delete(cookieName, cookieOptions);
        return Results.Ok(new { ok = true, message = "Logged out successfully" });
    }

    public static IResult PlayerLogout(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete("pl.token", new CookieOptions { Path = "/" });
        return TypedResults.Ok(new LogoutResponse(Success: true, Message: "Logged out successfully"));
    }

    public static async Task<IResult> GetAdminProfile(
        HttpContext httpContext,
        CasinoDbContext db,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        
        try
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("Invalid user ID in JWT token: {UserIdClaim}", userIdClaim);
                return Results.Problem("Invalid user ID in token", statusCode: 401);
            }

            var user = await db.BackofficeUsers
                .Include(u => u.Brand)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                logger.LogWarning("User not found for ID: {UserId}", userId);
                return Results.Problem("User not found", statusCode: 404);
            }

            var profile = new
            {
                user.Id,
                user.Username,
                Role = user.Role.ToString(),
                Brand = user.Brand != null ? new { user.Brand.Id, user.Brand.Name, user.Brand.Code } : null,
                user.LastLoginAt
            };

            return TypedResults.Ok(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting admin profile");
            return Results.Problem("Internal server error", statusCode: 500);
        }
    }

    public static async Task<IResult> GetPlayerProfile(
        HttpContext httpContext,
        CasinoDbContext db,
        BrandContext brandContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AuthEndpoints");
        
        try
        {
            var playerIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(playerIdClaim, out var playerId))
            {
                logger.LogWarning("Invalid player ID in JWT token: {PlayerIdClaim}", playerIdClaim);
                return Results.Problem("Invalid player ID in token", statusCode: 401);
            }

            var player = await db.Players
                .Include(p => p.Brand)
                .Include(p => p.Wallet)
                .FirstOrDefaultAsync(p => p.Id == playerId && p.BrandId == brandContext.BrandId);

            if (player == null)
            {
                logger.LogWarning("Player not found or not authorized for brand: {PlayerId} - {BrandCode}", 
                    playerId, brandContext.BrandCode);
                return Results.Problem("Player not found or not authorized for this brand", statusCode: 404);
            }

            var profile = new
            {
                player.Id,
                player.Username,
                player.Email,
                Brand = new { player.Brand.Code, player.Brand.Name },
                Balance = player.Wallet?.BalanceBigint ?? 0,
                Status = player.Status.ToString()
            };

            return TypedResults.Ok(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting player profile");
            return Results.Problem("Internal server error", statusCode: 500);
        }
    }
}