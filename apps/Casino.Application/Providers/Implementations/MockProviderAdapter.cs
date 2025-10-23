using Microsoft.Extensions.Logging;

namespace Casino.Application.Providers.Implementations;

/// <summary>
/// Adapter mock para pruebas locales
/// Genera URLs a un servidor demo local
/// </summary>
public class MockProviderAdapter : IProviderAdapter
{
    private readonly ILogger<MockProviderAdapter> _logger;
    
    public MockProviderAdapter(ILogger<MockProviderAdapter> logger)
    {
   _logger = logger;
    }
    
    public string ProviderCode => "mock";
    
    public Task<GameLaunchResponse> LaunchGameAsync(
        LaunchGameRequest request, 
     CancellationToken cancellationToken = default)
    {
        try
  {
         _logger.LogInformation("Mock provider launching game: {GameCode} for player {PlayerId}", 
     request.GameCode, request.PlayerId);
        
     // Generar token mock
       var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
      .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
      
            // Construir URL mock
      var launchUrl = $"https://demo.local/games/{request.GameCode}" +
 $"?session={token}" +
       $"&player={request.PlayerId}" +
          $"&balance={request.PlayerBalance}" +
         $"&demo={request.IsDemo.ToString().ToLower()}";
       
 if (!string.IsNullOrEmpty(request.ReturnUrl))
            {
  launchUrl += $"&returnUrl={Uri.EscapeDataString(request.ReturnUrl)}";
          }
  
      _logger.LogInformation("Mock launch URL generated: {LaunchUrl}", launchUrl);
      
        return Task.FromResult(new GameLaunchResponse(
      Success: true,
      LaunchUrl: launchUrl,
       SessionToken: token,
   ExpiresAt: DateTime.UtcNow.AddMinutes(60),
       ErrorMessage: null
            ));
 }
        catch (Exception ex)
   {
       _logger.LogError(ex, "Error in mock provider launch for game {GameCode}", request.GameCode);
     
return Task.FromResult(new GameLaunchResponse(
       Success: false,
           LaunchUrl: null,
     SessionToken: null,
    ExpiresAt: null,
         ErrorMessage: $"Mock provider error: {ex.Message}"
       ));
      }
    }
    
    public Task<bool> ValidateSessionAsync(
        string sessionToken, 
        CancellationToken cancellationToken = default)
    {
// Mock: siempre válido si tiene formato correcto
        var isValid = !string.IsNullOrWhiteSpace(sessionToken) && sessionToken.Length > 10;
      
    _logger.LogInformation("Mock session validation: {Token} -> {IsValid}", 
      sessionToken, isValid);
  
        return Task.FromResult(isValid);
    }
}
