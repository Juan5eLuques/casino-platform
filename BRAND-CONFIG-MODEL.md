# Brand Config Model

## Entidad Brand (EF Core)
```csharp
public class Brand {
  public Guid Id { get; set; }
  public Guid OperatorId { get; set; }
  public string Code { get; set; } = default!;
  public string Name { get; set; } = default!;
  public string? Domain { get; set; }
  public string? AdminDomain { get; set; }
  public string Status { get; set; } = "ACTIVE"; // ACTIVE|INACTIVE
  public string[] CorsOrigins { get; set; } = Array.Empty<string>();
  public JsonDocument Theme { get; set; } = JsonDocument.Parse("{}");
  public JsonDocument Settings { get; set; } = JsonDocument.Parse("{}");
  public DateTime CreatedAt { get; set; }
}

public class BrandProviderConfig {
  public Guid BrandId { get; set; }
  public string ProviderCode { get; set; } = default!;
  public string Secret { get; set; } = default!;
  public bool AllowNegativeOnRollback { get; set; } = false;
  public JsonDocument Meta { get; set; } = JsonDocument.Parse("{}");
  public DateTime UpdatedAt { get; set; }
}