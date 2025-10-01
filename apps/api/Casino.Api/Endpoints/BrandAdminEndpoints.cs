using Casino.Application.DTOs.Brand;
using Casino.Application.DTOs.Game;
using Casino.Application.Services;
using Casino.Application.Services.Models;
using Casino.Application.Mappers;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Endpoints;

public static class BrandAdminEndpoints
{
    public static void MapBrandAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var brandGroup = app.MapGroup("/api/v1/admin/brands")
            .WithTags("Brand Admin");

        var settingsGroup = app.MapGroup("/api/v1/admin/brands/{brandId:guid}/settings")
            .WithTags("Brand Settings");

        var providersGroup = app.MapGroup("/api/v1/admin/brands/{brandId:guid}/providers")
            .WithTags("Brand Provider Config");

        // Brand CRUD
        brandGroup.MapPost("/", CreateBrand)
            .WithName("CreateBrand")
            .WithSummary("Create a new brand")
            .Produces<GetBrandResponse>(201)
            .Produces(409)
            .ProducesValidationProblem();

        brandGroup.MapGet("/", GetBrands)
            .WithName("GetBrands")
            .WithSummary("Get brands with filtering and pagination")
            .Produces<QueryBrandsResponse>();

        brandGroup.MapGet("/{brandId:guid}", GetBrand)
            .WithName("GetBrand")
            .WithSummary("Get brand by ID")
            .Produces<GetBrandResponse>()
            .Produces(404);

        brandGroup.MapPatch("/{brandId:guid}", UpdateBrand)
            .WithName("UpdateBrand")
            .WithSummary("Update brand")
            .Produces<GetBrandResponse>()
            .Produces(404)
            .ProducesValidationProblem();

        brandGroup.MapDelete("/{brandId:guid}", DeleteBrand)
            .WithName("DeleteBrand")
            .WithSummary("Delete brand")
            .Produces(200)
            .Produces(404)
            .Produces(409);

        brandGroup.MapPost("/{brandId:guid}/status", UpdateBrandStatus)
            .WithName("UpdateBrandStatus")
            .WithSummary("Update brand status")
            .Produces<GetBrandResponse>()
            .Produces(404);

        // Utility endpoints
        brandGroup.MapGet("/by-host/{host}", GetBrandByHost)
            .WithName("GetBrandByHost")
            .WithSummary("Get brand by host/domain")
            .Produces<GetBrandResponse>()
            .Produces(404);

        brandGroup.MapGet("/{brandId:guid}/catalog", GetBrandCatalog)
            .WithName("GetBrandCatalog")
            .WithSummary("Get brand game catalog")
            .Produces<IEnumerable<CatalogGameResponse>>()
            .Produces(404);

        // Settings endpoints
        settingsGroup.MapGet("/", GetBrandSettings)
            .WithName("GetBrandSettings")
            .WithSummary("Get brand settings")
            .Produces<Dictionary<string, object>>()
            .Produces(404);

        settingsGroup.MapPut("/", UpdateBrandSettings)
            .WithName("UpdateBrandSettings")
            .WithSummary("Replace brand settings")
            .Produces<Dictionary<string, object>>()
            .Produces(404);

        settingsGroup.MapPatch("/", PatchBrandSettings)
            .WithName("PatchBrandSettings")
            .WithSummary("Patch brand settings")
            .Produces<Dictionary<string, object>>()
            .Produces(404);

        // Provider config endpoints
        providersGroup.MapGet("/", GetBrandProviders)
            .WithName("GetBrandProviders")
            .WithSummary("Get brand provider configurations")
            .Produces<GetBrandProvidersResponse>()
            .Produces(404);

        providersGroup.MapPut("/{providerCode}", UpsertProviderConfig)
            .WithName("UpsertProviderConfig")
            .WithSummary("Create or update provider configuration")
            .Produces<GetProviderConfigResponse>()
            .Produces(404);

        providersGroup.MapPost("/{providerCode}/rotate-secret", RotateProviderSecret)
            .WithName("RotateProviderSecret")
            .WithSummary("Rotate provider secret key")
            .Produces<RotateSecretResponse>()
            .Produces(404);
    }

    private static async Task<IResult> CreateBrand(
        [FromBody] CreateBrandRequest request,
        IBrandService brandService,
        IValidator<CreateBrandRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            // TODO: Get current user ID from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder

            var response = await brandService.CreateBrandAsync(request, currentUserId);
            
            logger.LogInformation("Brand created: {BrandId} - {Code}", response.Id, response.Code);
            
            return TypedResults.Created($"/api/v1/admin/brands/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand creation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Creation Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating brand with code: {Code}", request.Code);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating brand",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrands(
        [AsParameters] QueryBrandsRequest request,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get operator scope from JWT/auth context
            Guid? operatorScope = null; // For SUPER_ADMIN, null means access to all

            var response = await brandService.GetBrandsAsync(request, operatorScope);
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting brands");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brands",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrand(
        Guid brandId,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get operator scope from JWT/auth context
            Guid? operatorScope = null;

            var response = await brandService.GetBrandAsync(brandId, operatorScope);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "Brand Not Found",
                    detail: "Brand does not exist or access denied",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting brand: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brand",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateBrand(
        Guid brandId,
        [FromBody] UpdateBrandRequest request,
        IBrandService brandService,
        IValidator<UpdateBrandRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            // TODO: Get current user ID and operator scope from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            Guid? operatorScope = null;

            var response = await brandService.UpdateBrandAsync(brandId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Brand updated: {BrandId} - {Code}", response.Id, response.Code);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand update failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Update Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating brand: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating brand",
                statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteBrand(
        Guid brandId,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get current user ID and operator scope from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            Guid? operatorScope = null;

            var success = await brandService.DeleteBrandAsync(brandId, currentUserId, operatorScope);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Brand Not Found",
                    detail: "Brand does not exist or access denied",
                    statusCode: 404);
            }

            logger.LogInformation("Brand deleted: {BrandId}", brandId);
            
            return TypedResults.Ok(new { Success = true, Message = "Brand deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand deletion failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Deletion Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting brand: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while deleting brand",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateBrandStatus(
        Guid brandId,
        [FromBody] UpdateBrandStatusRequest request,
        IBrandService brandService,
        IValidator<UpdateBrandStatusRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            // TODO: Get current user ID and operator scope from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            Guid? operatorScope = null;

            var response = await brandService.UpdateBrandStatusAsync(brandId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Brand status updated: {BrandId} to {Status}", brandId, request.Status);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand status update failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Status Update Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating brand status: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating brand status",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrandByHost(
        string host,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            var response = await brandService.GetBrandByHostAsync(host);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "Brand Not Found",
                    detail: $"No brand found for host '{host}'",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting brand by host: {Host}", host);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brand by host",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrandCatalog(
        Guid brandId,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get operator scope from JWT/auth context
            Guid? operatorScope = null;

            var gamesResult = await brandService.GetBrandCatalogAsync(brandId, operatorScope);
            
            // Mapear a DTOs
            var games = gamesResult.Select(g => new CatalogGameResponse(
                g.GameId,
                g.Code,
                g.Name,
                g.Provider,
                g.Enabled,
                g.DisplayOrder,
                g.Tags));
            
            return TypedResults.Ok(games);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand catalog access failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Not Found",
                detail: ex.Message,
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting brand catalog: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brand catalog",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrandSettings(
        Guid brandId,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get operator scope from JWT/auth context
            Guid? operatorScope = null;

            var response = await brandService.GetBrandSettingsAsync(brandId, operatorScope);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "Brand Not Found",
                    detail: "Brand does not exist or access denied",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting brand settings: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brand settings",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateBrandSettings(
        Guid brandId,
        [FromBody] UpdateBrandSettingsRequest request,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get current user ID and operator scope from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            Guid? operatorScope = null;

            var response = await brandService.UpdateBrandSettingsAsync(brandId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Brand settings updated: {BrandId}", brandId);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand settings update failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Settings Update Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating brand settings: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating brand settings",
                statusCode: 500);
        }
    }

    private static async Task<IResult> PatchBrandSettings(
        Guid brandId,
        [FromBody] PatchBrandSettingsRequest request,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get current user ID and operator scope from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            Guid? operatorScope = null;

            var response = await brandService.PatchBrandSettingsAsync(brandId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Brand settings patched: {BrandId}", brandId);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand settings patch failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Settings Patch Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error patching brand settings: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while patching brand settings",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBrandProviders(
        Guid brandId,
        IBrandService brandService,
        ILogger<Program> logger)
    {
        try
        {
            // TODO: Get operator scope from JWT/auth context
            Guid? operatorScope = null;

            var response = await brandService.GetBrandProvidersAsync(brandId, operatorScope);
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Brand providers access failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Brand Not Found",
                detail: ex.Message,
                statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting brand providers: {BrandId}", brandId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting brand providers",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpsertProviderConfig(
        Guid brandId,
        string providerCode,
        [FromBody] UpsertProviderConfigRequest request,
        IBrandService brandService,
        IValidator<UpsertProviderConfigRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            // TODO: Get current user ID and operator scope from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            Guid? operatorScope = null;

            var response = await brandService.UpsertProviderConfigAsync(brandId, providerCode, request, currentUserId, operatorScope);
            
            logger.LogInformation("Provider config upserted: {BrandId} - {ProviderCode}", brandId, providerCode);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Provider config upsert failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Provider Config Upsert Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error upserting provider config: {BrandId} - {ProviderCode}", brandId, providerCode);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while upserting provider config",
                statusCode: 500);
        }
    }

    private static async Task<IResult> RotateProviderSecret(
        Guid brandId,
        string providerCode,
        [FromBody] RotateProviderSecretRequest request,
        IBrandService brandService,
        IValidator<RotateProviderSecretRequest> validator,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            // TODO: Get current user ID and operator scope from JWT/auth context
            var currentUserId = Guid.NewGuid(); // Placeholder
            Guid? operatorScope = null;

            var response = await brandService.RotateProviderSecretAsync(brandId, providerCode, request, currentUserId, operatorScope);
            
            logger.LogInformation("Provider secret rotated: {BrandId} - {ProviderCode}", brandId, providerCode);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Provider secret rotation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Provider Secret Rotation Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 400);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rotating provider secret: {BrandId} - {ProviderCode}", brandId, providerCode);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while rotating provider secret",
                statusCode: 500);
        }
    }
}