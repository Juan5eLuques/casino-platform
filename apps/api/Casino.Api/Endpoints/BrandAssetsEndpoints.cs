using Casino.Api.Middleware;
using Casino.Application.DTOs.BrandAssets;
using Casino.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class BrandAssetsEndpoints
{
    public static void MapBrandAssetsEndpoints(this RouteGroupBuilder group)
    {
        var assetsGroup = group.MapGroup("/brands/assets")
       .WithTags("Brand Assets")
.RequireAuthorization("BackofficePolicy");

        // Initialize brand assets structure
        assetsGroup.MapPost("/initialize", InitializeAssets)
        .WithName("InitializeBrandAssets")
  .WithSummary("Initialize S3 folder structure for a brand")
            .WithDescription("Initialize brand assets structure. Accepts optional 'brandId' query parameter. If not provided, resolves brand from Host header.")
   .Produces<InitializeAssetsResponse>()
      .Produces(400)
   .Produces(401);

 // Upload banner
        assetsGroup.MapPost("/upload/banner/{section}", UploadBanner)
            .WithName("UploadBanner")
      .WithSummary("Upload a banner image to a specific section (home, slots, live-casino)")
      .Produces<UploadBannerResponse>()
.Produces(400)
          .Produces(401)
          .DisableAntiforgery();

    // Upload media
 assetsGroup.MapPost("/upload/media/{type}", UploadMedia)
     .WithName("UploadMedia")
    .WithSummary("Upload media file (logo, favicon, other)")
            .Produces<UploadMediaResponse>()
            .Produces(400)
            .Produces(401)
        .DisableAntiforgery();

        // Delete banner
        assetsGroup.MapDelete("/banner/{section}/{fileName}", DeleteBanner)
   .WithName("DeleteBanner")
    .WithSummary("Delete a banner from a section")
 .Produces<bool>()
.Produces(400)
            .Produces(401);

        // Delete media
        assetsGroup.MapDelete("/media/{type}", DeleteMedia)
  .WithName("DeleteMedia")
.WithSummary("Delete media file (logo or favicon)")
            .Produces<bool>()
  .Produces(400)
    .Produces(401);

        // Publish config.js
        assetsGroup.MapPost("/publish-config", PublishConfig)
 .WithName("PublishConfig")
          .WithSummary("Generate and publish config.js file to S3")
       .Produces<PublishConfigResponse>()
  .Produces(400)
     .Produces(401);

        // Get brand settings
        assetsGroup.MapGet("/settings", GetSettings)
            .WithName("GetBrandAssetSettings")
     .WithSummary("Get current brand settings (colors, images, config URL)")
            .Produces<BrandSettingsResponse>()
         .Produces(400)
            .Produces(401);

        // Update colors
        assetsGroup.MapPut("/colors", UpdateColors)
  .WithName("UpdateBrandColors")
  .WithSummary("Update brand color palette")
            .Produces<BrandSettingsResponse>()
            .Produces(400)
      .Produces(401);
    }

    private static async Task<IResult> InitializeAssets(
        BrandContext brandContext,
        IBrandAssetsService assetsService,
 ClaimsPrincipal user,
   ILogger<Program> logger,
        [FromQuery] Guid? brandId = null)
    {
     try
        {
  // Determine which brandId to use: explicit parameter or resolved from Host
            Guid targetBrandId;

            if (brandId.HasValue)
      {
           // Use explicit brandId from query parameter
    targetBrandId = brandId.Value;
          logger.LogInformation("Initializing brand assets using explicit brandId: {BrandId}", targetBrandId);
            }
            else
            {
       // Fallback to brand resolved from Host header
    if (!brandContext.IsResolved)
    return Results.BadRequest(new { error = "Brand context not resolved. Provide brandId parameter or valid Host header" });
                
         targetBrandId = brandContext.BrandId;
     logger.LogInformation("Initializing brand assets using Host-resolved brandId: {BrandId}", targetBrandId);
            }

     var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
      ?? throw new UnauthorizedAccessException("User ID not found"));

var response = await assetsService.InitializeBrandAssetsAsync(targetBrandId, currentUserId);
  return Results.Ok(response);
   }
  catch (Exception ex)
    {
          logger.LogError(ex, "Failed to initialize brand assets");
            return Results.Problem(
            title: "Initialization Failed",
       detail: ex.Message,
                statusCode: 500);
        }
    }

  private static async Task<IResult> UploadBanner(
    string section,
        HttpRequest request,
        BrandContext brandContext,
        IBrandAssetsService assetsService,
 ClaimsPrincipal user,
        ILogger<Program> logger)
    {
        try
        {
            if (!brandContext.IsResolved)
    return Results.BadRequest(new { error = "Brand context not resolved" });

 // Debug: Log all form fields
            logger.LogInformation("Form has {Count} files", request.Form.Files.Count);
 foreach (var file in request.Form.Files)
   {
        logger.LogInformation("File field name: {Name}, FileName: {FileName}, Size: {Size}", 
     file.Name, file.FileName, file.Length);
  }

            // Get file from request - try all possible field names
            IFormFile? uploadedFile = null;
 
            // Try common field names
    var possibleNames = new[] { "file", "File", "files", "Files", "upload", "image" };
    foreach (var name in possibleNames)
     {
                uploadedFile = request.Form.Files.GetFile(name);
        if (uploadedFile != null)
      {
      logger.LogInformation("Found file with field name: {Name}", name);
                  break;
         }
    }

    // If still not found, try the first file
            if (uploadedFile == null && request.Form.Files.Count > 0)
 {
   uploadedFile = request.Form.Files[0];
          logger.LogInformation("Using first file: {Name}", uploadedFile.Name);
 }

        if (uploadedFile == null || uploadedFile.Length == 0)
   {
       var errorMsg = request.HasFormContentType 
      ? $"No file found. Available fields: {string.Join(", ", request.Form.Files.Select(f => f.Name))}"
       : "Invalid content type. Expected multipart/form-data";
       
                logger.LogWarning("Upload failed: {Error}", errorMsg);
  return Results.BadRequest(new { error = errorMsg });
       }

    var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
   ?? throw new UnauthorizedAccessException("User ID not found"));

            var response = await assetsService.UploadBannerAsync(brandContext.BrandId, section, uploadedFile, currentUserId);
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
       return Results.BadRequest(new { error = ex.Message });
        }
    catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
    }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload banner");
     return Results.Problem(
 title: "Upload Failed",
detail: ex.Message,
          statusCode: 500);
        }
    }

    private static async Task<IResult> UploadMedia(
        string type,
        HttpRequest request,
        BrandContext brandContext,
   IBrandAssetsService assetsService,
        ClaimsPrincipal user,
        ILogger<Program> logger)
    {
        try
        {
   if (!brandContext.IsResolved)
     return Results.BadRequest(new { error = "Brand context not resolved" });

       // Debug: Log all form fields
  logger.LogInformation("Form has {Count} files", request.Form.Files.Count);
    foreach (var file in request.Form.Files)
         {
       logger.LogInformation("File field name: {Name}, FileName: {FileName}, Size: {Size}", 
       file.Name, file.FileName, file.Length);
       }

// Get file from request - try all possible field names
            IFormFile? uploadedFile = null;

            // Try common field names
    var possibleNames = new[] { "file", "File", "files", "Files", "upload", "image" };
         foreach (var name in possibleNames)
       {
              uploadedFile = request.Form.Files.GetFile(name);
   if (uploadedFile != null)
       {
     logger.LogInformation("Found file with field name: {Name}", name);
    break;
         }
   }

      // If still not found, try the first file
     if (uploadedFile == null && request.Form.Files.Count > 0)
            {
    uploadedFile = request.Form.Files[0];
        logger.LogInformation("Using first file: {Name}", uploadedFile.Name);
            }

          if (uploadedFile == null || uploadedFile.Length == 0)
            {
     var errorMsg = request.HasFormContentType 
            ? $"No file found. Available fields: {string.Join(", ", request.Form.Files.Select(f => f.Name))}"
      : "Invalid content type. Expected multipart/form-data";
         
   logger.LogWarning("Upload failed: {Error}", errorMsg);
          return Results.BadRequest(new { error = errorMsg });
            }

            var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
       ?? throw new UnauthorizedAccessException("User ID not found"));

    var response = await assetsService.UploadMediaAsync(brandContext.BrandId, type, uploadedFile, currentUserId);
        return Results.Ok(response);
   }
    catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
         logger.LogError(ex, "Failed to upload media");
     return Results.Problem(
     title: "Upload Failed",
              detail: ex.Message,
    statusCode: 500);
     }
    }

 private static async Task<IResult> DeleteBanner(
        string section,
    string fileName,
        BrandContext brandContext,
      IBrandAssetsService assetsService,
   ClaimsPrincipal user,
        ILogger<Program> logger)
    {
        try
        {
if (!brandContext.IsResolved)
  return Results.BadRequest(new { error = "Brand context not resolved" });

       var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
           ?? throw new UnauthorizedAccessException("User ID not found"));

   var result = await assetsService.DeleteBannerAsync(brandContext.BrandId, section, fileName, currentUserId);
            
  if (!result)
       return Results.NotFound(new { error = "Banner not found" });

            return Results.Ok(new { success = true, message = "Banner deleted successfully" });
        }
   catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete banner");
            return Results.Problem(
   title: "Delete Failed",
    detail: ex.Message,
    statusCode: 500);
   }
    }

    private static async Task<IResult> DeleteMedia(
        string type,
  BrandContext brandContext,
  IBrandAssetsService assetsService,
        ClaimsPrincipal user,
     ILogger<Program> logger)
    {
        try
        {
   if (!brandContext.IsResolved)
          return Results.BadRequest(new { error = "Brand context not resolved" });

            var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
         ?? throw new UnauthorizedAccessException("User ID not found"));

    var result = await assetsService.DeleteMediaAsync(brandContext.BrandId, type, currentUserId);

            if (!result)
           return Results.NotFound(new { error = "Media not found" });

            return Results.Ok(new { success = true, message = "Media deleted successfully" });
        }
        catch (Exception ex)
        {
    logger.LogError(ex, "Failed to delete media");
         return Results.Problem(
                title: "Delete Failed",
            detail: ex.Message,
             statusCode: 500);
  }
    }

    private static async Task<IResult> PublishConfig(
        BrandContext brandContext,
        IBrandAssetsService assetsService,
        ClaimsPrincipal user,
        ILogger<Program> logger)
    {
        try
        {
  if (!brandContext.IsResolved)
                return Results.BadRequest(new { error = "Brand context not resolved" });

  var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found"));

            var response = await assetsService.PublishConfigAsync(brandContext.BrandId, currentUserId);
            return Results.Ok(response);
     }
      catch (Exception ex)
  {
            logger.LogError(ex, "Failed to publish config");
    return Results.Problem(
         title: "Publish Failed",
  detail: ex.Message,
   statusCode: 500);
   }
    }

    private static async Task<IResult> GetSettings(
        BrandContext brandContext,
        IBrandAssetsService assetsService,
    ILogger<Program> logger)
    {
        try
        {
  if (!brandContext.IsResolved)
      return Results.BadRequest(new { error = "Brand context not resolved" });

          var response = await assetsService.GetBrandSettingsAsync(brandContext.BrandId);
        return Results.Ok(response);
        }
        catch (Exception ex)
  {
      logger.LogError(ex, "Failed to get brand settings");
   return Results.Problem(
                title: "Get Settings Failed",
                detail: ex.Message,
   statusCode: 500);
    }
    }

    private static async Task<IResult> UpdateColors(
BrandContext brandContext,
        IBrandAssetsService assetsService,
      ClaimsPrincipal user,
    UpdateColorsRequest request,
     ILogger<Program> logger)
    {
      try
  {
    if (!brandContext.IsResolved)
                return Results.BadRequest(new { error = "Brand context not resolved" });

            var currentUserId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
         ?? throw new UnauthorizedAccessException("User ID not found"));

            var response = await assetsService.UpdateColorsAsync(brandContext.BrandId, request, currentUserId);
 return Results.Ok(response);
        }
 catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update colors");
       return Results.Problem(
                title: "Update Failed",
      detail: ex.Message,
        statusCode: 500);
        }
    }
}
