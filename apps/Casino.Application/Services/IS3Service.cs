namespace Casino.Application.Services;

/// <summary>
/// Service for AWS S3 operations
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Initialize folder structure for a brand
    /// </summary>
    Task InitializeBrandFoldersAsync(string brandName);
    
    /// <summary>
 /// Upload a file to S3
    /// </summary>
    Task<string> UploadFileAsync(string key, Stream fileStream, string contentType);
    
    /// <summary>
    /// Delete a file from S3
    /// </summary>
    Task<bool> DeleteFileAsync(string key);
    
    /// <summary>
    /// Check if a file exists in S3
    /// </summary>
    Task<bool> FileExistsAsync(string key);
    
    /// <summary>
    /// List files in a folder
    /// </summary>
    Task<List<string>> ListFilesAsync(string prefix);
    
    /// <summary>
    /// Get public URL for a file
    /// </summary>
    string GetPublicUrl(string key);
}
