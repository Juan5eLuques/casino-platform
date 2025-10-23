using Casino.Application.DTOs.Session;
using Casino.Application.Providers;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

/// <summary>
/// Implementación del servicio de lanzamiento de juegos
/// Coordina la creación de sesiones, llamadas a adapters y logging
/// </summary>
public class GameLaunchService : IGameLaunchService
{
    private readonly CasinoDbContext _context;
    private readonly ISessionService _sessionService;
 private readonly IProviderAdapterFactory _adapterFactory;
    private readonly ILogger<GameLaunchService> _logger;
    
    public GameLaunchService(
        CasinoDbContext context,
   ISessionService sessionService,
IProviderAdapterFactory adapterFactory,
ILogger<GameLaunchService> logger)
    {
_context = context;
        _sessionService = sessionService;
_adapterFactory = adapterFactory;
        _logger = logger;
    }

    public async Task<GameLaunchResponse> LaunchGameAsync(
   string gameCode, 
      Guid playerId, 
   Guid brandId, 
      bool isDemo = false, 
        CancellationToken cancellationToken = default)
    {
     _logger.LogInformation("Starting game launch: {GameCode} for player {PlayerId} in brand {BrandId}", 
            gameCode, playerId, brandId);
        
     try
        {
  // 1. Obtener juego con proveedor
   var game = await _context.Games
         .Include(g => g.ProviderEntity)
        .FirstOrDefaultAsync(g => g.Code == gameCode && g.Enabled, cancellationToken);
      
  if (game == null)
        {
 _logger.LogWarning("Game not found or disabled: {GameCode}", gameCode);
    return new GameLaunchResponse(false, null, null, null, "Game not found or disabled");
   }
   
       _logger.LogInformation("Game found: {GameCode}, Provider: {Provider}", gameCode, game.Provider);
  
        // 2. Verificar que el juego esté asignado al brand
    var brandGame = await _context.BrandGames
 .FirstOrDefaultAsync(bg => bg.BrandId == brandId && bg.GameId == game.Id && bg.Enabled, cancellationToken);
    
          if (brandGame == null)
            {
     _logger.LogWarning("Game {GameCode} not available for brand {BrandId}", gameCode, brandId);
  return new GameLaunchResponse(false, null, null, null, "Game not available for this brand");
    }
   
        _logger.LogInformation("Game {GameCode} is available for brand {BrandId}", gameCode, brandId);
  
     // 3. Obtener configuración del proveedor
      var providerConfig = await _context.BrandProviderConfigs
    .FirstOrDefaultAsync(c => c.BrandId == brandId && c.ProviderCode == game.Provider, cancellationToken);
    
 if (providerConfig == null)
       {
    _logger.LogWarning("Provider {Provider} not configured for brand {BrandId}", game.Provider, brandId);
     return new GameLaunchResponse(false, null, null, null, $"Provider '{game.Provider}' not configured for this brand");
    }
    
    _logger.LogInformation("Provider config found for {Provider}", game.Provider);
   
    // 4. Obtener player
     var player = await _context.Players
   .FirstOrDefaultAsync(p => p.Id == playerId && p.BrandId == brandId, cancellationToken);
 
          if (player == null)
{
     _logger.LogWarning("Player {PlayerId} not found or not in brand {BrandId}", playerId, brandId);
 return new GameLaunchResponse(false, null, null, null, "Player not found");
   }

    _logger.LogInformation("Player found: {Username}, Balance: {Balance}", player.Username, player.WalletBalance);
    
 // 5. Crear sesión de juego
var sessionRequest = new CreateSessionRequest(playerId, gameCode, game.Provider, 60);
      var session = await _sessionService.CreateSessionAsync(sessionRequest);
   
   _logger.LogInformation("Game session created: {SessionId}", session.SessionId);
    
    // 6. Obtener adapter del proveedor
       var adapter = _adapterFactory.GetAdapter(game.Provider);
            if (adapter == null)
     {
  _logger.LogError("Provider adapter not found: {Provider}", game.Provider);
      return new GameLaunchResponse(false, null, null, null, $"Provider '{game.Provider}' not supported");
            }
       
       _logger.LogInformation("Using adapter: {ProviderCode}", adapter.ProviderCode);
 
      // 7. Llamar al adapter para generar launch URL
   var launchRequest = new LaunchGameRequest(
           game.Code,
         game.LaunchId ?? game.Code,
     playerId,
    player.Username,
    player.WalletBalance,
          providerConfig.Secret,
      isDemo,
   null,
    providerConfig.Meta
     );
   
       var launchResponse = await adapter.LaunchGameAsync(launchRequest, cancellationToken);
    
      _logger.LogInformation("Launch response from adapter: Success={Success}, URL={Url}", 
launchResponse.Success, launchResponse.LaunchUrl);
      
    // 8. Guardar log de launch
  var launchLog = new GameLaunchLog
        {
   Id = Guid.NewGuid(),
    SessionId = session.SessionId,
         PlayerId = playerId,
GameId = game.Id,
            BrandId = brandId,
   Provider = game.Provider,
         LaunchUrl = launchResponse.LaunchUrl ?? "",
        SessionToken = launchResponse.SessionToken ?? "",
 Success = launchResponse.Success,
  ErrorMessage = launchResponse.ErrorMessage,
         CreatedAt = DateTime.UtcNow
    };
 
      _context.GameLaunchLogs.Add(launchLog);
 await _context.SaveChangesAsync(cancellationToken);
  
       _logger.LogInformation("Game launch {Status}: {GameCode} for player {PlayerId}, Log ID: {LogId}", 
      launchResponse.Success ? "SUCCESS" : "FAILED", gameCode, playerId, launchLog.Id);
  
            return launchResponse;
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error launching game {GameCode} for player {PlayerId}", gameCode, playerId);
    return new GameLaunchResponse(false, null, null, null, $"Internal error: {ex.Message}");
        }
    }
    
    public async Task<GameLaunchLog?> GetLaunchLogAsync(
   Guid sessionId, 
        CancellationToken cancellationToken = default)
    {
    return await _context.GameLaunchLogs
     .Include(l => l.Session)
 .Include(l => l.Player)
       .Include(l => l.Game)
   .Include(l => l.Brand)
      .FirstOrDefaultAsync(l => l.SessionId == sessionId, cancellationToken);
    }
    
    public async Task<IEnumerable<GameLaunchLog>> GetPlayerLaunchLogsAsync(
        Guid playerId, 
        int limit = 10, 
     CancellationToken cancellationToken = default)
    {
   return await _context.GameLaunchLogs
            .Include(l => l.Game)
 .Include(l => l.Session)
       .Where(l => l.PlayerId == playerId)
   .OrderByDescending(l => l.CreatedAt)
          .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
