using Casino.Application.Services;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

/// <summary>
/// Endpoints para obtener el balance del usuario logueado
/// Soporta tanto BACKOFFICE como PLAYER JWT
/// </summary>
public static class BalanceEndpoints
{
    public static void MapBalanceEndpoints(this IEndpointRouteBuilder app)
    {
    // GET /api/v1/balance - Obtener balance del usuario logueado
        app.MapGet("/api/v1/balance", GetMyBalance)
   .RequireAuthorization("AnyAuthenticatedUser") // ? Acepta BackofficeJwt O PlayerJwt
 .WithName("GetMyBalance")
   .WithTags("Balance")
            .WithSummary("Get balance of the currently logged-in user")
   .WithDescription("Returns the balance of the authenticated user (works for both BACKOFFICE and PLAYER)")
            .Produces<Casino.Application.DTOs.Balance.UserBalanceResponse>()
     .Produces(401)
      .Produces(404);
    }

    /// <summary>
 /// Obtiene el balance del usuario logueado automáticamente
    /// </summary>
    private static async Task<IResult> GetMyBalance(
        HttpContext httpContext,
 IBalanceService balanceService,
        ILogger<Program> logger)
    {
        try
     {
            // Detectar el tipo de usuario desde los claims del token
   var userType = DetectUserType(httpContext);
          var userId = ExtractUserId(httpContext, userType);

            if (userId == Guid.Empty)
      {
      logger.LogWarning("Failed to extract user ID from token");
   return Results.Unauthorized();
   }

       logger.LogInformation("Balance requested by {UserType} user: {UserId}", userType, userId);

      var balance = await balanceService.GetMyBalanceAsync(userId, userType);

            return Results.Ok(balance);
    }
  catch (InvalidOperationException ex)
  {
   logger.LogWarning("User not found: {Message}", ex.Message);
return Results.NotFound(new { error = "user_not_found", message = ex.Message });
        }
        catch (Exception ex)
     {
            logger.LogError(ex, "Error getting balance");
  return Results.Problem("An unexpected error occurred", statusCode: 500);
        }
  }

    /// <summary>
    /// Detecta si el usuario es BACKOFFICE o PLAYER basándose en el audience del JWT
    /// </summary>
 private static string DetectUserType(HttpContext httpContext)
    {
        var user = httpContext.User;

     // Verificar el claim de audience para determinar el tipo
        var audience = user.FindFirst("aud")?.Value;
        
    if (audience == "backoffice")
{
            return "BACKOFFICE";
     }
        else if (audience == "player")
 {
   return "PLAYER";
  }

        // Fallback: verificar por rol
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        
     if (role == "PLAYER")
   {
       return "PLAYER";
        }
        else if (role == "SUPER_ADMIN" || role == "BRAND_ADMIN" || role == "CASHIER")
   {
            return "BACKOFFICE";
 }

        // Default: asumir BACKOFFICE si está autenticado
  return "BACKOFFICE";
  }

    /// <summary>
 /// Extrae el UserId del token según el tipo de usuario
    /// </summary>
 private static Guid ExtractUserId(HttpContext httpContext, string userType)
    {
 var user = httpContext.User;

        if (userType == "BACKOFFICE")
        {
       // Para BACKOFFICE: usar NameIdentifier o sub claim
     var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
           ?? user.FindFirst("sub")?.Value;
      
            if (Guid.TryParse(userIdClaim, out var userId))
            {
return userId;
            }
   }
     else if (userType == "PLAYER")
        {
       // Para PLAYER: usar NameIdentifier (player_id en el token)
     var playerIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
         ?? user.FindFirst("player_id")?.Value
     ?? user.FindFirst("sub")?.Value;
      
            if (Guid.TryParse(playerIdClaim, out var playerId))
       {
          return playerId;
       }
        }

        return Guid.Empty;
 }
}
