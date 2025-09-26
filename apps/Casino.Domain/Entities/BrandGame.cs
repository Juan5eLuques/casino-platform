namespace Casino.Domain.Entities;

public class BrandGame
{
    public Guid BrandId { get; set; }
    public Guid GameId { get; set; }
    public bool Enabled { get; set; }
    public int DisplayOrder { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    // Navigation properties
    public Brand Brand { get; set; } = null!;
    public Game Game { get; set; } = null!;
}