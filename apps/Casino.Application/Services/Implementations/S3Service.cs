using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Casino.Application.Services.Implementations;

public class S3Service : IS3Service
{
  private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3Service> _logger;
    private readonly string _bucketName;
private readonly string _region;

    public S3Service(
        IAmazonS3 s3Client,
      IConfiguration configuration,
      ILogger<S3Service> logger)
    {
        _s3Client = s3Client;
     _logger = logger;
    _bucketName = configuration["AWS:S3:BucketName"] ?? "brand-assets-prod";
        _region = configuration["AWS:S3:Region"] ?? "us-east-1";
    }

    public async Task InitializeBrandFoldersAsync(string brandName)
    {
        var folders = new[]
        {
  $"assets/{brandName}/banners/home/",
            $"assets/{brandName}/banners/slots/",
            $"assets/{brandName}/banners/live-casino/",
          $"assets/{brandName}/banners/media/",
          $"assets/{brandName}/config/"
  };

        foreach (var folder in folders)
        {
          try
            {
         // Create empty .init file to ensure folder exists
      var initKey = $"{folder}.init";
  await _s3Client.PutObjectAsync(new PutObjectRequest
      {
        BucketName = _bucketName,
  Key = initKey,
 ContentBody = "",
           ContentType = "text/plain"
     });

          _logger.LogInformation("Created folder: {Folder}", folder);
            }
         catch (Exception ex)
          {
          _logger.LogError(ex, "Failed to create folder: {Folder}", folder);
 throw;
    }
        }
    }

    public async Task<string> UploadFileAsync(string key, Stream fileStream, string contentType)
    {
      try
        {
       var request = new PutObjectRequest
      {
        BucketName = _bucketName,
         Key = key,
       InputStream = fileStream,
 ContentType = contentType
      // REMOVED: CannedACL = S3CannedACL.PublicRead
         // AWS now disables ACLs by default. Use Bucket Policy instead for public access.
         };

         await _s3Client.PutObjectAsync(request);
            
      var publicUrl = GetPublicUrl(key);
     _logger.LogInformation("File uploaded successfully: {Key} -> {Url}", key, publicUrl);

            return publicUrl;
        }
        catch (Exception ex)
 {
            _logger.LogError(ex, "Failed to upload file: {Key}", key);
    throw;
 }
    }

    public async Task<bool> DeleteFileAsync(string key)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
   {
     BucketName = _bucketName,
                Key = key
            });

      _logger.LogInformation("File deleted: {Key}", key);
 return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Key}", key);
  return false;
}
    }

    public async Task<bool> FileExistsAsync(string key)
    {
        try
        {
    await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
    {
           BucketName = _bucketName,
     Key = key
        });
         return true;
        }
      catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
  {
      return false;
}
    }

    public async Task<List<string>> ListFilesAsync(string prefix)
    {
        try
        {
      var request = new ListObjectsV2Request
 {
          BucketName = _bucketName,
     Prefix = prefix
     };

    var response = await _s3Client.ListObjectsV2Async(request);
         
      return response.S3Objects
     .Where(obj => !obj.Key.EndsWith("/") && !obj.Key.EndsWith(".init"))
       .Select(obj => obj.Key)
      .ToList();
        }
  catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files with prefix: {Prefix}", prefix);
    return new List<string>();
        }
    }

    public string GetPublicUrl(string key)
    {
        return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
    }
}
