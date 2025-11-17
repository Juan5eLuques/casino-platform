using System.Text.Json;

namespace Casino.Domain.Entities;

public class BrandSettings
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    
    /// <summary>
/// JSON object containing color settings: { "primaryColor": "#ff0000", "secondaryColor": "#00ff00", ... }
    /// </summary>
    public JsonDocument Colors { get; set; } = JsonDocument.Parse("{}");
    
    /// <summary>
    /// JSON object containing image URLs: 
    /// {
    ///   "banners": {
    ///     "home": ["url1", "url2"],
    ///     "slots": ["url1"],
    ///     "live-casino": []
    ///   },
    ///   "media": {
///     "logo": "url",
    ///     "favicon": "url",
    ///     "others": ["url1", "url2"]
    ///   }
    /// }
    /// </summary>
    public JsonDocument Images { get; set; } = JsonDocument.Parse("{\"banners\":{\"home\":[],\"slots\":[],\"live-casino\":[]},\"media\":{\"logo\":\"\",\"favicon\":\"\",\"others\":[]}}");
    
 public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public Brand Brand { get; set; } = null!;
}
