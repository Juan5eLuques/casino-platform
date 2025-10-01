using System.Text.Json;

namespace Casino.Domain.Entities;

public class BrandProviderConfig
{
    public Guid BrandId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool AllowNegativeOnRollback { get; set; } = false;
    public JsonDocument? Meta { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
}