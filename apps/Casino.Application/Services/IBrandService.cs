using Casino.Application.DTOs.Brand;
using Casino.Application.Services.Models;

namespace Casino.Application.Services;

public interface IBrandService
{
    // CRUD Operations
    Task<GetBrandResponse> CreateBrandAsync(CreateBrandRequest request, Guid currentUserId);
    Task<QueryBrandsResponse> GetBrandsAsync(QueryBrandsRequest request, Guid? brandScope = null);
    Task<GetBrandResponse?> GetBrandAsync(Guid brandId, Guid? brandScope = null);
    Task<GetBrandResponse> UpdateBrandAsync(Guid brandId, UpdateBrandRequest request, Guid currentUserId, Guid? brandScope = null);
    Task<bool> DeleteBrandAsync(Guid brandId, Guid currentUserId, Guid? brandScope = null);
    
    // Status Management
    Task<GetBrandResponse> UpdateBrandStatusAsync(Guid brandId, UpdateBrandStatusRequest request, Guid currentUserId, Guid? brandScope = null);
    
    // Settings Management
    Task<Dictionary<string, object>?> GetBrandSettingsAsync(Guid brandId, Guid? brandScope = null);
    Task<Dictionary<string, object>> UpdateBrandSettingsAsync(Guid brandId, UpdateBrandSettingsRequest request, Guid currentUserId, Guid? brandScope = null);
    Task<Dictionary<string, object>> PatchBrandSettingsAsync(Guid brandId, PatchBrandSettingsRequest request, Guid currentUserId, Guid? brandScope = null);
    
    // Provider Configuration
    Task<GetBrandProvidersResponse> GetBrandProvidersAsync(Guid brandId, Guid? brandScope = null);
    Task<GetProviderConfigResponse> UpsertProviderConfigAsync(Guid brandId, string providerCode, UpsertProviderConfigRequest request, Guid currentUserId, Guid? brandScope = null);
    Task<RotateSecretResponse> RotateProviderSecretAsync(Guid brandId, string providerCode, RotateProviderSecretRequest request, Guid currentUserId, Guid? brandScope = null);
    
    // Utilities
    Task<GetBrandResponse?> GetBrandByHostAsync(string host);
    Task<IEnumerable<GetBrandGameResult>> GetBrandCatalogAsync(Guid brandId, Guid? brandScope = null);
    
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