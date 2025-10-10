namespace Casino.Application.DTOs.Audit;

// Request DTOs
public record QueryBackofficeAuditRequest
{
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? BrandId { get; set; } // Para scoping automático por brand
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public record QueryProviderAuditRequest
{
    public string? Provider { get; set; }
    public string? Action { get; set; }
    public string? SessionId { get; set; }
    public string? PlayerId { get; set; }
    public string? GameCode { get; set; }
    public string? RoundId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? BrandId { get; set; } // Para scoping automático por brand
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// Response DTOs
public record QueryBackofficeAuditResponse(
    List<BackofficeAuditResponse> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record QueryProviderAuditResponse(
    List<ProviderAuditResponse> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record BackofficeAuditResponse(
    Guid Id,
    Guid UserId,
    string Username,
    string UserRole,
    string? OperatorName, // Keep for backward compatibility but will be null
    string Action,
    string TargetType,
    string TargetId,
    object? Meta,
    DateTime CreatedAt
);

public record ProviderAuditResponse(
    Guid Id,
    string Provider,
    string Action,
    string? SessionId,
    string? PlayerId,
    string? RoundId,
    string? GameCode,
    string? ExternalRef,
    object? RequestData,
    object? ResponseData,
    int StatusCode,
    DateTime CreatedAt
);

// Validation Request DTO for ValidateSiteSetupRequest
public record ValidateSiteSetupRequest
{
    public string? BrandCode { get; set; }
    public string? Domain { get; set; }
    public Guid? BrandId { get; set; } // Para scoping automático por brand
}

public record ValidateSiteSetupResponse(
    bool IsValid,
    string? BrandCode,
    string? Domain,
    List<ValidationIssue> Issues
);

public record ValidationIssue(
    string Type,
    string Message,
    string? Recommendation = null
);