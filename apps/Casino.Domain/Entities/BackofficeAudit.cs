using System.Text.Json;

namespace Casino.Domain.Entities;

public class BackofficeAudit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public JsonDocument? Meta { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public BackofficeUser User { get; set; } = null!;
}