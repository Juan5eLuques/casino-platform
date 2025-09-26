using System.Text.Json;

namespace Casino.Domain.Entities;

public class ProviderAudit
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? PlayerId { get; set; }
    public string? RoundId { get; set; }
    public string? ExternalRef { get; set; }
    public JsonDocument? RequestData { get; set; }
    public JsonDocument? ResponseData { get; set; }
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
}