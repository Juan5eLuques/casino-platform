namespace Casino.Application.Providers;

/// <summary>
/// Factory para obtener el adapter correcto según el código del proveedor
/// </summary>
public interface IProviderAdapterFactory
{
    /// <summary>
    /// Obtiene el adapter para un proveedor específico
    /// </summary>
    /// <param name="providerCode">Código del proveedor (ej: "pragmatic", "evolution", "mock")</param>
    /// <returns>Adapter del proveedor o null si no existe</returns>
  IProviderAdapter? GetAdapter(string providerCode);
    
    /// <summary>
/// Obtiene todos los adapters registrados
    /// </summary>
    IEnumerable<IProviderAdapter> GetAllAdapters();
}
