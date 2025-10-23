using Microsoft.Extensions.Logging;

namespace Casino.Application.Providers.Implementations;

/// <summary>
/// Factory implementation que usa dependency injection para obtener adapters
/// </summary>
public class ProviderAdapterFactory : IProviderAdapterFactory
{
    private readonly IEnumerable<IProviderAdapter> _adapters;
    private readonly ILogger<ProviderAdapterFactory> _logger;
    
    public ProviderAdapterFactory(
      IEnumerable<IProviderAdapter> adapters,
     ILogger<ProviderAdapterFactory> logger)
    {
    _adapters = adapters;
        _logger = logger;
        
        _logger.LogInformation("ProviderAdapterFactory initialized with {Count} adapters: {Codes}", 
       _adapters.Count(), 
            string.Join(", ", _adapters.Select(a => a.ProviderCode)));
    }
    
    public IProviderAdapter? GetAdapter(string providerCode)
    {
        var adapter = _adapters.FirstOrDefault(a => 
      a.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase));
  
        if (adapter == null)
        {
        _logger.LogWarning("Provider adapter not found: {ProviderCode}. Available: {Available}", 
                providerCode, 
                string.Join(", ", _adapters.Select(a => a.ProviderCode)));
  }
        
   return adapter;
    }
    
  public IEnumerable<IProviderAdapter> GetAllAdapters()
{
        return _adapters;
    }
}
