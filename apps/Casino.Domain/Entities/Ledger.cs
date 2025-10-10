using Casino.Domain.Enums;
using System.Text.Json;

namespace Casino.Domain.Entities;

public class Ledger
{
    public long Id { get; set; }
    public Guid BrandId { get; set; }
    public Guid PlayerId { get; set; }
    public long DeltaBigint { get; set; }
    public LedgerReason Reason { get; set; }
    public Guid? RoundId { get; set; }
    public string? GameCode { get; set; }
    public string? Provider { get; set; }
    public string? ExternalRef { get; set; }
    public JsonDocument? Meta { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Round? Round { get; set; }
}