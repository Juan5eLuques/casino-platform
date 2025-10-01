using Casino.Application.DTOs.Brand;
using Casino.Domain.Enums;
using FluentValidation;

namespace Casino.Application.Validators.Brand;

public class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9_]+$")
            .WithMessage("Code must contain only uppercase letters, numbers, and underscores");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Locale)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[a-z]{2}-[A-Z]{2}$")
            .WithMessage("Locale must be in format 'xx-XX' (e.g., 'en-US', 'es-ES')");

        RuleFor(x => x.Domain)
            .MaximumLength(255)
            .Must(BeValidDomain)
            .WithMessage("Domain must be a valid hostname")
            .When(x => !string.IsNullOrEmpty(x.Domain));

        RuleFor(x => x.AdminDomain)
            .MaximumLength(255)
            .Must(BeValidDomain)
            .WithMessage("AdminDomain must be a valid hostname")
            .When(x => !string.IsNullOrEmpty(x.AdminDomain));

        RuleFor(x => x.CorsOrigins)
            .Must(BeValidCorsOrigins)
            .WithMessage("All CORS origins must be valid URLs or 'localhost' patterns");
    }

    private static bool BeValidDomain(string? domain)
    {
        if (string.IsNullOrEmpty(domain)) return true;
        
        // Allow localhost patterns for development
        if (domain.StartsWith("localhost"))
            return true;
            
        // Basic domain validation
        return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
    }

    private static bool BeValidCorsOrigins(string[] origins)
    {
        if (origins == null || origins.Length == 0) return true;

        foreach (var origin in origins)
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            
            // Allow localhost patterns
            if (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost"))
                continue;
                
            // Must be valid URI for production origins
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                return false;
                
            // Must be HTTP or HTTPS
            if (uri.Scheme != "http" && uri.Scheme != "https")
                return false;
        }

        return true;
    }
}

public class UpdateBrandRequestValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(255)
            .When(x => x.Name != null);

        RuleFor(x => x.Locale)
            .MaximumLength(10)
            .Matches("^[a-z]{2}-[A-Z]{2}$")
            .WithMessage("Locale must be in format 'xx-XX' (e.g., 'en-US', 'es-ES')")
            .When(x => x.Locale != null);

        RuleFor(x => x.Domain)
            .MaximumLength(255)
            .Must(BeValidDomain)
            .WithMessage("Domain must be a valid hostname")
            .When(x => x.Domain != null);

        RuleFor(x => x.AdminDomain)
            .MaximumLength(255)
            .Must(BeValidDomain)
            .WithMessage("AdminDomain must be a valid hostname")
            .When(x => x.AdminDomain != null);

        RuleFor(x => x.CorsOrigins)
            .Must(BeValidCorsOrigins)
            .WithMessage("All CORS origins must be valid URLs")
            .When(x => x.CorsOrigins != null);
    }

    private static bool BeValidDomain(string? domain)
    {
        if (string.IsNullOrEmpty(domain)) return true;
        
        if (domain.StartsWith("localhost"))
            return true;
            
        return Uri.CheckHostName(domain) != UriHostNameType.Unknown;
    }

    private static bool BeValidCorsOrigins(string[]? origins)
    {
        if (origins == null) return true;

        foreach (var origin in origins)
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            
            if (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost"))
                continue;
                
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                return false;
                
            if (uri.Scheme != "http" && uri.Scheme != "https")
                return false;
        }

        return true;
    }
}

public class UpdateBrandStatusRequestValidator : AbstractValidator<UpdateBrandStatusRequest>
{
    public UpdateBrandStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be a valid BrandStatus value");
    }
}

public class UpsertProviderConfigRequestValidator : AbstractValidator<UpsertProviderConfigRequest>
{
    public UpsertProviderConfigRequestValidator()
    {
        RuleFor(x => x.Secret)
            .NotEmpty()
            .MinimumLength(16)
            .MaximumLength(500)
            .WithMessage("Secret must be between 16 and 500 characters");
    }
}

public class RotateProviderSecretRequestValidator : AbstractValidator<RotateProviderSecretRequest>
{
    public RotateProviderSecretRequestValidator()
    {
        RuleFor(x => x.SecretLength)
            .InclusiveBetween(16, 128)
            .WithMessage("Secret length must be between 16 and 128 characters");
    }
}