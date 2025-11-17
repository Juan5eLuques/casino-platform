using Microsoft.AspNetCore.Http;

namespace Casino.Application.DTOs.BrandAssets;

// === REQUEST DTOs ===

public record UpdateColorsRequest(
    Dictionary<string, string> Colors
);

// === RESPONSE DTOs ===

public record InitializeAssetsResponse(
    bool Success,
    string Message,
    List<string> FoldersCreated
);

public record UploadBannerResponse(
    bool Success,
    string Url,
    string Section,
    string FileName,
    int TotalBannersInSection
);

public record UploadMediaResponse(
    bool Success,
    string Url,
  string Type,
    string FileName
);

public record PublishConfigResponse(
    bool Success,
    string ConfigUrl,
    DateTime PublishedAt
);

public record BrandSettingsResponse(
    Guid BrandId,
    string BrandName,
    string BrandCode,
    Dictionary<string, string> Colors,
  BannerImages Banners,
    MediaImages Media,
    string? ConfigJsUrl,
    DateTime UpdatedAt
);

public record BannerImages(
    List<string> Home,
    List<string> Slots,
    List<string> LiveCasino
);

public record MediaImages(
    string? Logo,
    string? Favicon,
    List<string> Others
);

// === VALIDATION CONSTANTS ===

public static class BrandAssetsConstants
{
    public const int MaxBannersPerSection = 5;
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    
    public static readonly string[] ValidBannerSections = { "home", "slots", "live-casino" };
    public static readonly string[] ValidMediaTypes = { "logo", "favicon", "other" };
    
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
    public static readonly string[] AllowedImageContentTypes = 
    { 
        "image/jpeg", 
        "image/png", 
      "image/gif", 
     "image/webp",
        "image/svg+xml"
    };
}
