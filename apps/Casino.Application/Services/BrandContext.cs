namespace Casino.Application.Services;

public class BrandContext
{
    public Guid BrandId { get; set; }
    public string BrandCode { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string[] CorsOrigins { get; set; } = Array.Empty<string>();
    public Guid OperatorId { get; set; }

    public bool IsResolved => BrandId != Guid.Empty;
}