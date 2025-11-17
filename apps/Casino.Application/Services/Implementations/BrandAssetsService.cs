using Casino.Application.DTOs.BrandAssets;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Casino.Application.Services.Implementations;

public class BrandAssetsService : IBrandAssetsService
{
    private readonly CasinoDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly IBrandService _brandService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BrandAssetsService> _logger;

    public BrandAssetsService(
        CasinoDbContext context,
        IS3Service s3Service,
        IBrandService brandService,
        IAuditService auditService,
        ILogger<BrandAssetsService> logger)
    {
        _context = context;
        _s3Service = s3Service;
      _brandService = brandService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<InitializeAssetsResponse> InitializeBrandAssetsAsync(Guid brandId, Guid currentUserId)
    {
     // 1. Verify brand exists
        var brand = await _context.Brands.FindAsync(brandId);
        if (brand == null)
   throw new InvalidOperationException($"Brand {brandId} not found");

        // 2. Create S3 folder structure
        await _s3Service.InitializeBrandFoldersAsync(brand.Code);

  // 3. Create or update BrandSettings
        var settings = await _context.BrandSettings.FirstOrDefaultAsync(s => s.BrandId == brandId);
    
        if (settings == null)
        {
    settings = new BrandSettings
 {
                Id = Guid.NewGuid(),
              BrandId = brandId,
     Colors = JsonDocument.Parse("{}"),
        Images = JsonDocument.Parse("{\"banners\":{\"home\":[],\"slots\":[],\"live-casino\":[]},\"media\":{\"logo\":\"\",\"favicon\":\"\",\"others\":[]}}"),
         CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
            };
     _context.BrandSettings.Add(settings);
  }
        else
     {
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

      // 4. Audit log
 await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_ASSETS_INIT", "BrandSettings",
            brandId.ToString(), new { BrandCode = brand.Code });

        _logger.LogInformation("Brand assets initialized for: {BrandCode}", brand.Code);

        var foldersCreated = new List<string>
        {
   $"assets/{brand.Code}/banners/home/",
            $"assets/{brand.Code}/banners/slots/",
       $"assets/{brand.Code}/banners/live-casino/",
            $"assets/{brand.Code}/banners/media/",
          $"assets/{brand.Code}/config/"
        };

        return new InitializeAssetsResponse(true, "Brand assets initialized successfully", foldersCreated);
    }

    public async Task<UploadBannerResponse> UploadBannerAsync(Guid brandId, string section, Microsoft.AspNetCore.Http.IFormFile file, Guid currentUserId)
    {
        // 1. Validate section
    if (!BrandAssetsConstants.ValidBannerSections.Contains(section.ToLower()))
      throw new ArgumentException($"Invalid section. Must be one of: {string.Join(", ", BrandAssetsConstants.ValidBannerSections)}");

        // 2. Validate file
        ValidateImageFile(file);

    // 3. Get brand and settings
  var brand = await _context.Brands.FindAsync(brandId);
 if (brand == null)
 throw new InvalidOperationException($"Brand {brandId} not found");

        var settings = await GetOrCreateSettingsAsync(brandId);
     var images = ParseImagesJson(settings.Images);

      // 4. Check banner limit
        var sectionBanners = GetSectionBanners(images, section);
  if (sectionBanners.Count >= BrandAssetsConstants.MaxBannersPerSection)
            throw new InvalidOperationException($"Maximum {BrandAssetsConstants.MaxBannersPerSection} banners per section reached");

  // 5. Upload to S3
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var s3Key = $"assets/{brand.Code}/banners/{section}/{fileName}";
  
        using var stream = file.OpenReadStream();
    var publicUrl = await _s3Service.UploadFileAsync(s3Key, stream, file.ContentType);

     // 6. Update database
   sectionBanners.Add(publicUrl);
        SetSectionBanners(images, section, sectionBanners);
        settings.Images = JsonDocument.Parse(JsonSerializer.Serialize(images));
     settings.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        // 7. Audit log
        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_BANNER_UPLOAD", "BrandSettings",
            brandId.ToString(), new { Section = section, FileName = fileName, Url = publicUrl });

        _logger.LogInformation("Banner uploaded: {Brand} - {Section} - {FileName}", brand.Code, section, fileName);

        return new UploadBannerResponse(true, publicUrl, section, fileName, sectionBanners.Count);
    }

  public async Task<UploadMediaResponse> UploadMediaAsync(Guid brandId, string type, Microsoft.AspNetCore.Http.IFormFile file, Guid currentUserId)
    {
        // 1. Validate type
      type = type.ToLower();
        if (!BrandAssetsConstants.ValidMediaTypes.Contains(type))
            throw new ArgumentException($"Invalid media type. Must be one of: {string.Join(", ", BrandAssetsConstants.ValidMediaTypes)}");

        // 2. Validate file
 ValidateImageFile(file);

        // 3. Get brand and settings
     var brand = await _context.Brands.FindAsync(brandId);
        if (brand == null)
          throw new InvalidOperationException($"Brand {brandId} not found");

        var settings = await GetOrCreateSettingsAsync(brandId);
        var images = ParseImagesJson(settings.Images);

// 4. Delete old file if replacing logo/favicon
        if (type != "other")
        {
  var oldUrl = GetMediaUrl(images, type);
 if (!string.IsNullOrEmpty(oldUrl))
 {
      var oldKey = ExtractS3KeyFromUrl(oldUrl);
     await _s3Service.DeleteFileAsync(oldKey);
       }
        }

   // 5. Upload to S3
        var fileName = type == "other" 
            ? $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}"
            : $"{type}{Path.GetExtension(file.FileName)}";
        
   var s3Key = $"assets/{brand.Code}/banners/media/{fileName}";
  
        using var stream = file.OpenReadStream();
        var publicUrl = await _s3Service.UploadFileAsync(s3Key, stream, file.ContentType);

 // 6. Update database
 if (type == "logo")
 {
  var mediaDict = GetMediaDictionary(images);
            mediaDict["logo"] = publicUrl;
            images["media"] = mediaDict;
        }
        else if (type == "favicon")
 {
   var mediaDict = GetMediaDictionary(images);
  mediaDict["favicon"] = publicUrl;
        images["media"] = mediaDict;
        }
        else // other
    {
            var mediaDict = GetMediaDictionary(images);
            var others = mediaDict.ContainsKey("others") && mediaDict["others"] is JsonElement othersElement
           ? JsonSerializer.Deserialize<List<string>>(othersElement.GetRawText()) ?? new List<string>()
     : new List<string>();
            
      others.Add(publicUrl);
     mediaDict["others"] = others;
      images["media"] = mediaDict;
        }

        settings.Images = JsonDocument.Parse(JsonSerializer.Serialize(images));
        settings.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        // 7. Audit log
        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_MEDIA_UPLOAD", "BrandSettings",
 brandId.ToString(), new { Type = type, FileName = fileName, Url = publicUrl });

        _logger.LogInformation("Media uploaded: {Brand} - {Type} - {FileName}", brand.Code, type, fileName);

        return new UploadMediaResponse(true, publicUrl, type, fileName);
    }

    public async Task<bool> DeleteBannerAsync(Guid brandId, string section, string fileName, Guid currentUserId)
    {
   section = section.ToLower();
     if (!BrandAssetsConstants.ValidBannerSections.Contains(section))
   throw new ArgumentException($"Invalid section");

        var brand = await _context.Brands.FindAsync(brandId);
        if (brand == null)
       throw new InvalidOperationException($"Brand {brandId} not found");

      var settings = await _context.BrandSettings.FirstOrDefaultAsync(s => s.BrandId == brandId);
        if (settings == null)
        return false;

        var images = ParseImagesJson(settings.Images);
      var sectionBanners = GetSectionBanners(images, section);
        
        var bannerToRemove = sectionBanners.FirstOrDefault(url => url.Contains(fileName));
        if (bannerToRemove == null)
         return false;

        // Delete from S3
     var s3Key = ExtractS3KeyFromUrl(bannerToRemove);
        await _s3Service.DeleteFileAsync(s3Key);

        // Update database
        sectionBanners.Remove(bannerToRemove);
        SetSectionBanners(images, section, sectionBanners);
    settings.Images = JsonDocument.Parse(JsonSerializer.Serialize(images));
        settings.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

  await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_BANNER_DELETE", "BrandSettings",
   brandId.ToString(), new { Section = section, FileName = fileName });

        return true;
    }

    public async Task<bool> DeleteMediaAsync(Guid brandId, string type, Guid currentUserId)
    {
    type = type.ToLower();
    if (type == "other")
 throw new ArgumentException("Cannot delete 'other' type without specific file name");

        var brand = await _context.Brands.FindAsync(brandId);
  if (brand == null)
  throw new InvalidOperationException($"Brand {brandId} not found");

        var settings = await _context.BrandSettings.FirstOrDefaultAsync(s => s.BrandId == brandId);
      if (settings == null)
     return false;

     var images = ParseImagesJson(settings.Images);
        var oldUrl = GetMediaUrl(images, type);
        
        if (string.IsNullOrEmpty(oldUrl))
          return false;

        // Delete from S3
        var s3Key = ExtractS3KeyFromUrl(oldUrl);
        await _s3Service.DeleteFileAsync(s3Key);

      // Update database
        if (type == "logo")
   {
      var mediaDict = GetMediaDictionary(images);
  mediaDict["logo"] = "";
    images["media"] = mediaDict;
     }
        else if (type == "favicon")
        {
   var mediaDict = GetMediaDictionary(images);
            mediaDict["favicon"] = "";
    images["media"] = mediaDict;
   }

        settings.Images = JsonDocument.Parse(JsonSerializer.Serialize(images));
        settings.UpdatedAt = DateTime.UtcNow;
  
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_MEDIA_DELETE", "BrandSettings",
            brandId.ToString(), new { Type = type });

        return true;
    }

    public async Task<PublishConfigResponse> PublishConfigAsync(Guid brandId, Guid currentUserId)
    {
   var brand = await _context.Brands.FindAsync(brandId);
        if (brand == null)
    throw new InvalidOperationException($"Brand {brandId} not found");

        var settings = await GetOrCreateSettingsAsync(brandId);
        
        // Generate config.js content
        var configJs = GenerateConfigJs(brand, settings);
        
  // Upload to S3
        var s3Key = $"assets/{brand.Code}/config/config.js";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(configJs));
   var publicUrl = await _s3Service.UploadFileAsync(s3Key, stream, "application/javascript");

        // Audit log
        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_CONFIG_PUBLISH", "BrandSettings",
      brandId.ToString(), new { ConfigUrl = publicUrl });

        _logger.LogInformation("Config.js published for brand: {BrandCode} -> {Url}", brand.Code, publicUrl);

return new PublishConfigResponse(true, publicUrl, DateTime.UtcNow);
    }

    public async Task<BrandSettingsResponse> GetBrandSettingsAsync(Guid brandId)
    {
        var brand = await _context.Brands.FindAsync(brandId);
   if (brand == null)
    throw new InvalidOperationException($"Brand {brandId} not found");

        var settings = await GetOrCreateSettingsAsync(brandId);
        var images = ParseImagesJson(settings.Images);
      var colors = ParseColorsJson(settings.Colors);

    var configUrl = _s3Service.GetPublicUrl($"assets/{brand.Code}/config/config.js");
 
        // Try to check if config exists, but don't fail if we don't have read permissions
        bool configExists = false;
        try
{
            configExists = await _s3Service.FileExistsAsync($"assets/{brand.Code}/config/config.js");
        }
        catch (Exception ex)
        {
    // Log warning but don't fail - user might not have s3:GetObject permission
            _logger.LogWarning(ex, "Could not check if config.js exists for brand {BrandCode}. This is normal if S3 read permissions are not granted.", brand.Code);
        }

        return new BrandSettingsResponse(
     brandId,
      brand.Name,
  brand.Code,
   colors,
       new BannerImages(
        GetSectionBanners(images, "home"),
     GetSectionBanners(images, "slots"),
     GetSectionBanners(images, "live-casino")
         ),
     new MediaImages(
      GetMediaUrl(images, "logo"),
        GetMediaUrl(images, "favicon"),
     GetMediaOthers(images)
  ),
        configExists ? configUrl : null,
       settings.UpdatedAt
        );
    }

    public async Task<BrandSettingsResponse> UpdateColorsAsync(Guid brandId, UpdateColorsRequest request, Guid currentUserId)
    {
        var brand = await _context.Brands.FindAsync(brandId);
        if (brand == null)
            throw new InvalidOperationException($"Brand {brandId} not found");

  var settings = await GetOrCreateSettingsAsync(brandId);
        
        settings.Colors = JsonDocument.Parse(JsonSerializer.Serialize(request.Colors));
        settings.UpdatedAt = DateTime.UtcNow;
        
    await _context.SaveChangesAsync();

     await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_COLORS_UPDATE", "BrandSettings",
            brandId.ToString(), new { Colors = request.Colors });

        return await GetBrandSettingsAsync(brandId);
  }

    // === HELPER METHODS ===

    private async Task<BrandSettings> GetOrCreateSettingsAsync(Guid brandId)
    {
        var settings = await _context.BrandSettings.FirstOrDefaultAsync(s => s.BrandId == brandId);
        
      if (settings == null)
        {
            settings = new BrandSettings
            {
   Id = Guid.NewGuid(),
      BrandId = brandId,
    Colors = JsonDocument.Parse("{}"),
                Images = JsonDocument.Parse("{\"banners\":{\"home\":[],\"slots\":[],\"live-casino\":[]},\"media\":{\"logo\":\"\",\"favicon\":\"\",\"others\":[]}}"),
    CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
          };
            _context.BrandSettings.Add(settings);
        await _context.SaveChangesAsync();
 }

        return settings;
    }

    private Dictionary<string, object> ParseImagesJson(JsonDocument json)
    {
     return JsonSerializer.Deserialize<Dictionary<string, object>>(json.RootElement.GetRawText())
       ?? new Dictionary<string, object>();
    }

    private Dictionary<string, string> ParseColorsJson(JsonDocument json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json.RootElement.GetRawText())
   ?? new Dictionary<string, string>();
    }

    private List<string> GetSectionBanners(Dictionary<string, object> images, string section)
    {
        if (!images.ContainsKey("banners"))
      return new List<string>();

        var banners = images["banners"] as JsonElement? ?? JsonSerializer.SerializeToElement(images["banners"]);
    
        if (banners.ValueKind == JsonValueKind.Object && banners.TryGetProperty(section, out var sectionElement))
        {
    return JsonSerializer.Deserialize<List<string>>(sectionElement.GetRawText()) ?? new List<string>();
   }

   return new List<string>();
    }

    private void SetSectionBanners(Dictionary<string, object> images, string section, List<string> banners)
    {
        if (!images.ContainsKey("banners"))
     images["banners"] = new Dictionary<string, object>();

        var bannersDict = images["banners"] as Dictionary<string, object> 
      ?? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(images["banners"]))
        ?? new Dictionary<string, object>();

        bannersDict[section] = banners;
        images["banners"] = bannersDict;
    }

    private string? GetMediaUrl(Dictionary<string, object> images, string type)
    {
   if (!images.ContainsKey("media"))
            return null;

        var media = images["media"] as JsonElement? ?? JsonSerializer.SerializeToElement(images["media"]);
        
        if (media.ValueKind == JsonValueKind.Object && media.TryGetProperty(type, out var value))
      {
      var url = value.GetString();
       return string.IsNullOrEmpty(url) ? null : url;
    }

        return null;
    }

    private List<string> GetMediaOthers(Dictionary<string, object> images)
    {
        if (!images.ContainsKey("media"))
     return new List<string>();

        var media = images["media"] as JsonElement? ?? JsonSerializer.SerializeToElement(images["media"]);
        
        if (media.ValueKind == JsonValueKind.Object && media.TryGetProperty("others", out var others))
      {
return JsonSerializer.Deserialize<List<string>>(others.GetRawText()) ?? new List<string>();
        }

     return new List<string>();
    }

    private Dictionary<string, object> GetMediaDictionary(Dictionary<string, object> images)
{
  if (!images.ContainsKey("media"))
            return new Dictionary<string, object>();

      var media = images["media"] as JsonElement? ?? JsonSerializer.SerializeToElement(images["media"]);
        
        if (media.ValueKind == JsonValueKind.Object)
        {
  return JsonSerializer.Deserialize<Dictionary<string, object>>(media.GetRawText()) 
      ?? new Dictionary<string, object>();
        }

   return new Dictionary<string, object>();
    }

    private string ExtractS3KeyFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }

    private void ValidateImageFile(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null || file.Length == 0)
        throw new ArgumentException("File is required");

        if (file.Length > BrandAssetsConstants.MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds {BrandAssetsConstants.MaxFileSizeBytes / 1024 / 1024}MB limit");

    var extension = Path.GetExtension(file.FileName).ToLower();
      if (!BrandAssetsConstants.AllowedImageExtensions.Contains(extension))
       throw new ArgumentException($"Invalid file extension. Allowed: {string.Join(", ", BrandAssetsConstants.AllowedImageExtensions)}");

     if (!BrandAssetsConstants.AllowedImageContentTypes.Contains(file.ContentType.ToLower()))
 throw new ArgumentException($"Invalid content type: {file.ContentType}");
    }

    private string GenerateConfigJs(Brand brand, BrandSettings settings)
    {
        var images = ParseImagesJson(settings.Images);
  var colors = ParseColorsJson(settings.Colors);

        var sb = new StringBuilder();
  sb.AppendLine("// Brand Configuration - Auto-generated");
sb.AppendLine($"// Brand: {brand.Name} ({brand.Code})");
        sb.AppendLine($"// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
      
        // Brand name
        sb.AppendLine($"window.gBrandName = \"{brand.Code}\";");
  sb.AppendLine();

        // Colors
sb.AppendLine("window.gColors = {");
    if (colors.Any())
        {
            var colorEntries = colors.Select(c => $"  \"{c.Key}\": \"{c.Value}\"");
 sb.AppendLine(string.Join(",\n", colorEntries));
 }
   sb.AppendLine("};");
        sb.AppendLine();

        // Banners
        var homeBanners = GetSectionBanners(images, "home");
        var slotsBanners = GetSectionBanners(images, "slots");
 var liveCasinoBanners = GetSectionBanners(images, "live-casino");

        sb.AppendLine($"window.gHomeBannersDesktop = {JsonSerializer.Serialize(homeBanners)};");
        sb.AppendLine($"window.gSlotsBannersDesktop = {JsonSerializer.Serialize(slotsBanners)};");
        sb.AppendLine($"window.gLiveCasinoBannersDesktop = {JsonSerializer.Serialize(liveCasinoBanners)};");
        sb.AppendLine();

   // Media
        var logo = GetMediaUrl(images, "logo") ?? "";
  var favicon = GetMediaUrl(images, "favicon") ?? "";

        sb.AppendLine($"window.gLogo = \"{logo}\";");
        sb.AppendLine($"window.gFavicon = \"{favicon}\";");

        return sb.ToString();
    }
}
