using Casino.Domain.Enums;
using System.Text.Json;

namespace Casino.Domain.Entities;

public class Brand
{
    public Guid Id { get; set; }
    public Guid OperatorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string? Domain { get; set; } // Dominio principal del site
    public string? AdminDomain { get; set; } // Dominio opcional para backoffice
    public string[] CorsOrigins { get; set; } = Array.Empty<string>(); // Lista de orígenes permitidos
    public JsonDocument? Theme { get; set; }
    public JsonDocument? Settings { get; set; } // Configuración avanzada por site
    public BrandStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Operator Operator { get; set; } = null!;
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<BrandGame> BrandGames { get; set; } = new List<BrandGame>();
    public ICollection<Ledger> LedgerEntries { get; set; } = new List<Ledger>();
    public ICollection<BrandProviderConfig> ProviderConfigs { get; set; } = new List<BrandProviderConfig>();
}