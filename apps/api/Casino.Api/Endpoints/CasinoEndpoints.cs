using Casino.Api.Middleware;
using Casino.Application.Providers;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

/// <summary>
/// Endpoints públicos de casino para launch de juegos
/// </summary>
public static class CasinoEndpoints
{
    public static void MapCasinoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/casino")
       .WithTags("Casino");

        // ✅ Main launch endpoint: GET /casino/games/url/{provider}/{gameCode}
group.MapGet("/games/url/{provider}/{gameCode}", LaunchGameUrl)
      .WithName("LaunchGameUrl")
     .WithSummary("Launch game and redirect to provider iframe")
            .Produces(302)
            .Produces<LaunchGameErrorResponse>(400)
      .Produces<LaunchGameErrorResponse>(404);
    }

    private static async Task<IResult> LaunchGameUrl(
      string provider,
        string gameCode,
      BrandContext brandContext,
      IGameLaunchService launchService,
 ILogger<Program> logger,
   HttpContext httpContext,
    [FromQuery] string playerId,
  [FromQuery] bool demo = false)
    {
        // 1. Validar brand context
        if (!brandContext.IsResolved)
        {
      logger.LogWarning("Brand not resolved for launch request");
   return Results.Problem(
      title: "Brand Not Resolved",
    detail: "Brand context is not available",
                statusCode: 400);
     }

        // 2. Validar playerId
        if (!Guid.TryParse(playerId, out var playerGuid))
        {
logger.LogWarning("Invalid player ID format: {PlayerId}", playerId);
            return Results.Problem(
    title: "Invalid Player ID",
             detail: "Player ID must be a valid GUID",
              statusCode: 400);
        }

      logger.LogInformation(
      "Launch request: Game={GameCode}, Provider={Provider}, Player={PlayerId}, Brand={BrandCode}, Demo={Demo}",
      gameCode, provider, playerId, brandContext.BrandCode, demo);

        try
        {
          // 3. Llamar al servicio de launch
         var response = await launchService.LaunchGameAsync(
      gameCode,
         playerGuid,
           brandContext.BrandId,
     demo);

         // 4. Verificar si el launch fue exitoso
            if (!response.Success)
     {
        logger.LogWarning("Game launch failed: {ErrorMessage}", response.ErrorMessage);
     
              return Results.Json(
       new LaunchGameErrorResponse(
   Success: false,
   ErrorMessage: response.ErrorMessage ?? "Game launch failed",
         ErrorCode: DetermineErrorCode(response.ErrorMessage)),
             statusCode: 404);
            }

            // 5. Log del launch exitoso
         logger.LogInformation(
    "Game launched successfully: {GameCode} for player {PlayerId}, URL: {LaunchUrl}",
            gameCode, playerId, response.LaunchUrl);

    // ✅ OPCIÓN 1: Redirección 302 (RECOMENDADO para iframes)
        return Results.Redirect(response.LaunchUrl!);

            // OPCIÓN 2: Retornar JSON con la URL (descomentar si se prefiere)
            // return Results.Json(new LaunchGameSuccessResponse(
            //     Success: true,
            //     LaunchUrl: response.LaunchUrl!,
            //     SessionToken: response.SessionToken,
            //  ExpiresAt: response.ExpiresAt!.Value
            // ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error launching game {GameCode} for player {PlayerId}",
   gameCode, playerId);

       return Results.Problem(
       title: "Internal Server Error",
       detail: "An unexpected error occurred while launching the game",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Determina el código de error basado en el mensaje
 /// </summary>
    private static string DetermineErrorCode(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
        return "UNKNOWN_ERROR";

        if (errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
       return "GAME_NOT_FOUND";

        if (errorMessage.Contains("not available", StringComparison.OrdinalIgnoreCase))
   return "GAME_NOT_AVAILABLE";

if (errorMessage.Contains("not configured", StringComparison.OrdinalIgnoreCase))
            return "PROVIDER_NOT_CONFIGURED";

   if (errorMessage.Contains("not supported", StringComparison.OrdinalIgnoreCase))
   return "PROVIDER_NOT_SUPPORTED";

        if (errorMessage.Contains("Player not found", StringComparison.OrdinalIgnoreCase))
            return "PLAYER_NOT_FOUND";

      return "LAUNCH_FAILED";
    }

 // Response DTOs
    public record LaunchGameSuccessResponse(
        bool Success,
     string LaunchUrl,
        string? SessionToken,
        DateTime ExpiresAt
    );

    public record LaunchGameErrorResponse(
        bool Success,
  string ErrorMessage,
        string ErrorCode
    );
}
