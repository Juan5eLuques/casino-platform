using System.Security.Cryptography;
using System.Text;

namespace Casino.Api.Filters;

public class HmacEndpointFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HmacEndpointFilter> _logger;

    public HmacEndpointFilter(IConfiguration configuration, ILogger<HmacEndpointFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        // Get headers
        if (!request.Headers.TryGetValue("X-Provider", out var providerValues) ||
            !request.Headers.TryGetValue("X-Signature", out var signatureValues))
        {
            _logger.LogWarning("Missing required headers X-Provider or X-Signature");
            return Results.Problem(
                title: "Missing Headers",
                detail: "X-Provider and X-Signature headers are required",
                statusCode: 401);
        }

        var provider = providerValues.FirstOrDefault();
        var providedSignature = signatureValues.FirstOrDefault();

        if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(providedSignature))
        {
            _logger.LogWarning("Empty X-Provider or X-Signature headers");
            return Results.Problem(
                title: "Invalid Headers",
                detail: "X-Provider and X-Signature cannot be empty",
                statusCode: 401);
        }

        // Get provider secret from configuration
        var secret = _configuration[$"Providers:{provider}:Secret"];
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Unknown provider: {Provider}", provider);
            return Results.Problem(
                title: "Unknown Provider",
                detail: "Provider not configured",
                statusCode: 401);
        }

        // Read request body
        request.EnableBuffering();
        var body = await ReadRequestBodyAsync(request);
        request.Body.Position = 0; // Reset position for next middleware

        // Calculate expected signature
        var expectedSignature = CalculateHmacSha256(secret, body);

        // Compare signatures
        if (!string.Equals(providedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("HMAC signature mismatch for provider: {Provider}", provider);
            return Results.Problem(
                title: "Invalid Signature",
                detail: "HMAC signature verification failed",
                statusCode: 401);
        }

        _logger.LogInformation("HMAC signature verified for provider: {Provider}", provider);
        
        // Store provider in HttpContext for use by endpoints
        httpContext.Items["Provider"] = provider;

        return await next(context);
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static string CalculateHmacSha256(string secret, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}