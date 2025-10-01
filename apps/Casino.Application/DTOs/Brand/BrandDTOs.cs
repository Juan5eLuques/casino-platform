using Casino.Domain.Enums;
using System.Text.Json;

namespace Casino.Application.DTOs.Brand;

// Request DTOs
public record CreateBrandRequest(
    string Code,
    string Name,
    string Locale,
    string? Domain = null,
    string? AdminDomain = null,
    string[] CorsOrigins = null!,
    JsonDocument? Theme = null,
    JsonDocument? Settings = null)
{
    public string[] CorsOrigins { get; init; } = CorsOrigins ?? Array.Empty<string>();
}

public record UpdateBrandRequest(
    string? Name = null,
    string? Locale = null,
    string? Domain = null,
    string? AdminDomain = null,
    string[]? CorsOrigins = null,
    JsonDocument? Theme = null);

public record UpdateBrandStatusRequest(
    BrandStatus Status);

public record QueryBrandsRequest(
    Guid? OperatorId = null,
    BrandStatus? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50);

// Settings DTOs
public record UpdateBrandSettingsRequest(
    JsonDocument Settings);

public record PatchBrandSettingsRequest(
    Dictionary<string, object> Updates);

// Provider Config DTOs
public record UpsertProviderConfigRequest(
    string Secret,
    bool AllowNegativeOnRollback = false,
    JsonDocument? Meta = null);

public record RotateProviderSecretRequest(
    int SecretLength = 64);

// Response DTOs
public record GetBrandResponse(
    Guid Id,
    Guid OperatorId,
    string Code,
    string Name,
    string Locale,
    string? Domain,
    string? AdminDomain,
    string[] CorsOrigins,
    JsonDocument? Theme,
    JsonDocument? Settings,
    BrandStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    OperatorInfo? Operator = null);

public record OperatorInfo(
    Guid Id,
    string Name);

public record BrandSummaryResponse(
    Guid Id,
    string Code,
    string Name,
    string? Domain,
    BrandStatus Status,
    DateTime CreatedAt);

public record QueryBrandsResponse(
    IEnumerable<BrandSummaryResponse> Brands,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public record GetProviderConfigResponse(
    string ProviderCode,
    bool AllowNegativeOnRollback,
    JsonDocument? Meta,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool HasSecret = true);

public record GetBrandProvidersResponse(
    IEnumerable<GetProviderConfigResponse> Providers);

public record RotateSecretResponse(
    string NewSecret,
    DateTime UpdatedAt);