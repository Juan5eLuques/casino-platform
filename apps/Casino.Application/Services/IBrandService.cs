using Casino.Application.DTOs.Brand;
using Casino.Application.Services.Models;

namespace Casino.Application.Services;

public interface IBrandService
{
    // CRUD Operations
    Task<GetBrandResponse> CreateBrandAsync(CreateBrandRequest request, Guid currentUserId);
    Task<QueryBrandsResponse> GetBrandsAsync(QueryBrandsRequest request, Guid? operatorScope = null);
    Task<GetBrandResponse?> GetBrandAsync(Guid brandId, Guid? operatorScope = null);
    Task<GetBrandResponse> UpdateBrandAsync(Guid brandId, UpdateBrandRequest request, Guid currentUserId, Guid? operatorScope = null);
    Task<bool> DeleteBrandAsync(Guid brandId, Guid currentUserId, Guid? operatorScope = null);
    
    // Status Management
    Task<GetBrandResponse> UpdateBrandStatusAsync(Guid brandId, UpdateBrandStatusRequest request, Guid currentUserId, Guid? operatorScope = null);
    
    // Settings Management
    Task<Dictionary<string, object>?> GetBrandSettingsAsync(Guid brandId, Guid? operatorScope = null);
    Task<Dictionary<string, object>> UpdateBrandSettingsAsync(Guid brandId, UpdateBrandSettingsRequest request, Guid currentUserId, Guid? operatorScope = null);
    Task<Dictionary<string, object>> PatchBrandSettingsAsync(Guid brandId, PatchBrandSettingsRequest request, Guid currentUserId, Guid? operatorScope = null);
    
    // Provider Configuration
    Task<GetBrandProvidersResponse> GetBrandProvidersAsync(Guid brandId, Guid? operatorScope = null);
    Task<GetProviderConfigResponse> UpsertProviderConfigAsync(Guid brandId, string providerCode, UpsertProviderConfigRequest request, Guid currentUserId, Guid? operatorScope = null);
    Task<RotateSecretResponse> RotateProviderSecretAsync(Guid brandId, string providerCode, RotateProviderSecretRequest request, Guid currentUserId, Guid? operatorScope = null);
    
    // Utilities
    Task<GetBrandResponse?> GetBrandByHostAsync(string host);
    Task<IEnumerable<GetBrandGameResult>> GetBrandCatalogAsync(Guid brandId, Guid? operatorScope = null);
    
    // Cache invalidation
    Task InvalidateBrandCacheAsync(Guid brandId);
}

public record GetBrandGameResult(
    Guid GameId,
    string Code,
    string Name,
    string Provider,
    bool Enabled,
    int DisplayOrder,
    string[] Tags);