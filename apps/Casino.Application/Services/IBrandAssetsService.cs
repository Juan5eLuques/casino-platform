using Casino.Application.DTOs.BrandAssets;
using Microsoft.AspNetCore.Http;

namespace Casino.Application.Services;

/// <summary>
/// Service for managing brand assets (banners, logos, media, config.js)
/// </summary>
public interface IBrandAssetsService
{
    /// <summary>
    /// Initialize complete folder structure for a brand
    /// </summary>
    Task<InitializeAssetsResponse> InitializeBrandAssetsAsync(Guid brandId, Guid currentUserId);
    
 /// <summary>
    /// Upload banner to a specific section
    /// </summary>
    Task<UploadBannerResponse> UploadBannerAsync(Guid brandId, string section, Microsoft.AspNetCore.Http.IFormFile file, Guid currentUserId);
    
/// <summary>
    /// Upload media file (logo, favicon, other)
    /// </summary>
    Task<UploadMediaResponse> UploadMediaAsync(Guid brandId, string type, Microsoft.AspNetCore.Http.IFormFile file, Guid currentUserId);
    
    /// <summary>
    /// Delete a banner from a section
    /// </summary>
    Task<bool> DeleteBannerAsync(Guid brandId, string section, string fileName, Guid currentUserId);
    
    /// <summary>
    /// Delete a media file
    /// </summary>
    Task<bool> DeleteMediaAsync(Guid brandId, string type, Guid currentUserId);
    
    /// <summary>
    /// Generate and publish config.js file
    /// </summary>
    Task<PublishConfigResponse> PublishConfigAsync(Guid brandId, Guid currentUserId);
    
    /// <summary>
    /// Get current brand settings
    /// </summary>
    Task<BrandSettingsResponse> GetBrandSettingsAsync(Guid brandId);
    
    /// <summary>
    /// Update brand colors
    /// </summary>
    Task<BrandSettingsResponse> UpdateColorsAsync(Guid brandId, UpdateColorsRequest request, Guid currentUserId);
}
