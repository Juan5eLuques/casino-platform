using Casino.Application.DTOs.Brand;
using Casino.Application.Services;
using Casino.Application.Services.Models;
using Casino.Domain.Entities;
using Casino.Domain.Enums;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Casino.Application.Services.Implementations;

public class BrandService : IBrandService
{
    private readonly CasinoDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<BrandService> _logger;

    public BrandService(CasinoDbContext context, IAuditService auditService, ILogger<BrandService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GetBrandResponse> CreateBrandAsync(CreateBrandRequest request, Guid currentUserId)
    {
        _logger.LogInformation("Creating brand with code: {Code}", request.Code);

        // Validate required fields first
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Brand code is required");
        
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Brand name is required");
        
        if (string.IsNullOrWhiteSpace(request.Locale))
            throw new ArgumentException("Brand locale is required");

        // Check for duplicates
        var existingCode = await _context.Brands.AsNoTracking()
            .AnyAsync(b => b.Code == request.Code);
        if (existingCode)
            throw new InvalidOperationException($"Brand with code '{request.Code}' already exists");

        if (!string.IsNullOrEmpty(request.Domain))
        {
            var existingDomain = await _context.Brands.AsNoTracking()
                .AnyAsync(b => b.Domain == request.Domain);
            if (existingDomain)
                throw new InvalidOperationException($"Brand with domain '{request.Domain}' already exists");
        }

        if (!string.IsNullOrEmpty(request.AdminDomain))
        {
            var existingAdminDomain = await _context.Brands.AsNoTracking()
                .AnyAsync(b => b.AdminDomain == request.AdminDomain);
            if (existingAdminDomain)
                throw new InvalidOperationException($"Brand with admin domain '{request.AdminDomain}' already exists");
        }

        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Locale = request.Locale,
            Domain = request.Domain,
            AdminDomain = request.AdminDomain,
            CorsOrigins = request.CorsOrigins,
            Theme = request.Theme,
            Settings = request.Settings,
            Status = BrandStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_CREATE", "Brand", 
            brand.Id.ToString(), new { request.Code, request.Name, request.Domain });

        _logger.LogInformation("Brand created successfully: {BrandId} - {Code}", brand.Id, brand.Code);

        return await GetBrandResponseAsync(brand);
    }

    public async Task<QueryBrandsResponse> GetBrandsAsync(QueryBrandsRequest request, Guid? brandScope = null)
    {
        var query = _context.Brands.AsNoTracking();

        // Apply brand scope
        if (brandScope.HasValue)
            query = query.Where(b => b.Id == brandScope.Value);

        // Apply filters
        if (request.Status.HasValue)
            query = query.Where(b => b.Status == request.Status.Value);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(b => 
                b.Code.ToLower().Contains(search) ||
                b.Name.ToLower().Contains(search) ||
                (b.Domain != null && b.Domain.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();
        
        var brands = await query
            .OrderBy(b => b.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BrandSummaryResponse(
                b.Id,
                b.Code,
                b.Name,
                b.Domain,
                b.Status,
                b.CreatedAt))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new QueryBrandsResponse(brands, request.Page, request.PageSize, totalCount, totalPages);
    }

    public async Task<GetBrandResponse?> GetBrandAsync(Guid brandId, Guid? brandScope = null)
    {
        var query = _context.Brands.AsNoTracking();

        if (brandScope.HasValue)
            query = query.Where(b => b.Id == brandScope.Value);

        var brand = await query.FirstOrDefaultAsync(b => b.Id == brandId);
        
        return brand != null ? await GetBrandResponseAsync(brand) : null;
    }

    public async Task<GetBrandResponse> UpdateBrandAsync(Guid brandId, UpdateBrandRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var brand = await GetBrandForUpdateAsync(brandId, brandScope);
        
        var oldValues = new { brand.Name, brand.Locale, brand.Domain, brand.AdminDomain, brand.CorsOrigins };
        bool invalidateCache = false;

        // Update fields
        if (request.Name != null)
            brand.Name = request.Name;

        if (request.Locale != null)
            brand.Locale = request.Locale;

        if (request.Domain != null && request.Domain != brand.Domain)
        {
            await ValidateUniqueDomainAsync(request.Domain, brandId);
            brand.Domain = request.Domain;
            invalidateCache = true;
        }

        if (request.AdminDomain != null && request.AdminDomain != brand.AdminDomain)
        {
            await ValidateUniqueAdminDomainAsync(request.AdminDomain, brandId);
            brand.AdminDomain = request.AdminDomain;
            invalidateCache = true;
        }

        if (request.CorsOrigins != null && !request.CorsOrigins.SequenceEqual(brand.CorsOrigins))
        {
            brand.CorsOrigins = request.CorsOrigins;
            invalidateCache = true;
        }

        if (request.Theme != null)
            brand.Theme = request.Theme;

        brand.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_UPDATE", "Brand",
            brand.Id.ToString(), new { OldValues = oldValues, NewValues = request });

        if (invalidateCache)
            await InvalidateBrandCacheAsync(brandId);

        _logger.LogInformation("Brand updated: {BrandId} - {Code}", brandId, brand.Code);

        return await GetBrandResponseAsync(brand);
    }

    public async Task<bool> DeleteBrandAsync(Guid brandId, Guid currentUserId, Guid? brandScope = null)
    {
        var brand = await GetBrandForUpdateAsync(brandId, brandScope);

        // Check if brand has active players or sessions
        var hasActivePlayers = await _context.Players.AnyAsync(p => p.BrandId == brandId && p.Status == PlayerStatus.ACTIVE);
        if (hasActivePlayers)
            throw new InvalidOperationException("Cannot delete brand with active players");

        var hasActiveSessions = await _context.GameSessions.AnyAsync(s => s.Player.BrandId == brandId && s.Status == GameSessionStatus.OPEN);
        if (hasActiveSessions)
            throw new InvalidOperationException("Cannot delete brand with active game sessions");

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_DELETE", "Brand",
            brand.Id.ToString(), new { brand.Code, brand.Name });

        await InvalidateBrandCacheAsync(brandId);

        _logger.LogInformation("Brand deleted: {BrandId} - {Code}", brandId, brand.Code);

        return true;
    }

    public async Task<GetBrandResponse> UpdateBrandStatusAsync(Guid brandId, UpdateBrandStatusRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var brand = await GetBrandForUpdateAsync(brandId, brandScope);
        
        var oldStatus = brand.Status;
        brand.Status = request.Status;
        brand.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_STATUS_UPDATE", "Brand",
            brand.Id.ToString(), new { OldStatus = oldStatus, NewStatus = request.Status });

        _logger.LogInformation("Brand status updated: {BrandId} - {Code} to {Status}", brandId, brand.Code, request.Status);

        return await GetBrandResponseAsync(brand);
    }

    public async Task<Dictionary<string, object>?> GetBrandSettingsAsync(Guid brandId, Guid? brandScope = null)
    {
        var query = _context.Brands.AsNoTracking();

        if (brandScope.HasValue)
            query = query.Where(b => b.Id == brandScope.Value);

        var brand = await query.FirstOrDefaultAsync(b => b.Id == brandId);
        if (brand == null) return null;

        if (brand.Settings == null) return new Dictionary<string, object>();

        return JsonSerializer.Deserialize<Dictionary<string, object>>(brand.Settings.RootElement.GetRawText()) ?? new Dictionary<string, object>();
    }

    public async Task<Dictionary<string, object>> UpdateBrandSettingsAsync(Guid brandId, UpdateBrandSettingsRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var brand = await GetBrandForUpdateAsync(brandId, brandScope);

        var oldSettings = brand.Settings;
        brand.Settings = request.Settings;
        brand.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_SETTINGS_PUT", "Brand",
            brand.Id.ToString(), new { OldSettings = oldSettings, NewSettings = request.Settings });

        _logger.LogInformation("Brand settings updated: {BrandId} - {Code}", brandId, brand.Code);

        return JsonSerializer.Deserialize<Dictionary<string, object>>(request.Settings.RootElement.GetRawText()) ?? new Dictionary<string, object>();
    }

    public async Task<Dictionary<string, object>> PatchBrandSettingsAsync(Guid brandId, PatchBrandSettingsRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        var brand = await GetBrandForUpdateAsync(brandId, brandScope);

        var currentSettings = brand.Settings != null 
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(brand.Settings.RootElement.GetRawText()) ?? new Dictionary<string, object>()
            : new Dictionary<string, object>();

        var oldSettings = JsonDocument.Parse(JsonSerializer.Serialize(currentSettings));

        // Apply patches
        foreach (var update in request.Updates)
        {
            currentSettings[update.Key] = update.Value;
        }

        brand.Settings = JsonDocument.Parse(JsonSerializer.Serialize(currentSettings));
        brand.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_SETTINGS_PATCH", "Brand",
            brand.Id.ToString(), new { OldSettings = oldSettings, Updates = request.Updates });

        _logger.LogInformation("Brand settings patched: {BrandId} - {Code}", brandId, brand.Code);

        return currentSettings;
    }

    public async Task<GetBrandProvidersResponse> GetBrandProvidersAsync(Guid brandId, Guid? brandScope = null)
    {
        // Verify brand access
        await GetBrandAsync(brandId, brandScope);

        var providers = await _context.BrandProviderConfigs
            .Where(c => c.BrandId == brandId)
            .AsNoTracking()
            .Select(c => new GetProviderConfigResponse(
                c.ProviderCode,
                c.AllowNegativeOnRollback,
                c.Meta,
                c.CreatedAt,
                c.UpdatedAt,
                true))
            .ToListAsync();

        return new GetBrandProvidersResponse(providers);
    }

    public async Task<GetProviderConfigResponse> UpsertProviderConfigAsync(Guid brandId, string providerCode, UpsertProviderConfigRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        // Verify brand access
        await GetBrandAsync(brandId, brandScope);

        var config = await _context.BrandProviderConfigs
            .FirstOrDefaultAsync(c => c.BrandId == brandId && c.ProviderCode == providerCode);

        var isUpdate = config != null;

        if (config == null)
        {
            config = new BrandProviderConfig
            {
                BrandId = brandId,
                ProviderCode = providerCode,
                CreatedAt = DateTime.UtcNow
            };
            _context.BrandProviderConfigs.Add(config);
        }

        config.Secret = request.Secret;
        config.AllowNegativeOnRollback = request.AllowNegativeOnRollback;
        config.Meta = request.Meta;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_PROVIDER_CONFIG_UPSERT", "BrandProviderConfig",
            $"{brandId}|{providerCode}", new { IsUpdate = isUpdate, ProviderCode = providerCode, request.AllowNegativeOnRollback });

        _logger.LogInformation("Brand provider config upserted: {BrandId} - {ProviderCode}", brandId, providerCode);

        return new GetProviderConfigResponse(
            config.ProviderCode,
            config.AllowNegativeOnRollback,
            config.Meta,
            config.CreatedAt,
            config.UpdatedAt,
            true);
    }

    public async Task<RotateSecretResponse> RotateProviderSecretAsync(Guid brandId, string providerCode, RotateProviderSecretRequest request, Guid currentUserId, Guid? brandScope = null)
    {
        // Verify brand access
        await GetBrandAsync(brandId, brandScope);

        var config = await _context.BrandProviderConfigs
            .FirstOrDefaultAsync(c => c.BrandId == brandId && c.ProviderCode == providerCode);

        if (config == null)
            throw new InvalidOperationException($"Provider config not found for brand {brandId} and provider {providerCode}");

        var newSecret = GenerateSecretKey(request.SecretLength);
        var oldSecretHash = HashSecret(config.Secret);

        config.Secret = newSecret;
        config.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogBackofficeActionAsync(currentUserId, "BRAND_PROVIDER_SECRET_ROTATE", "BrandProviderConfig",
            $"{brandId}|{providerCode}", new { ProviderCode = providerCode, OldSecretHash = oldSecretHash });

        _logger.LogInformation("Brand provider secret rotated: {BrandId} - {ProviderCode}", brandId, providerCode);

        return new RotateSecretResponse(newSecret, config.UpdatedAt);
    }

    public async Task<GetBrandResponse?> GetBrandByHostAsync(string host)
    {
        var brand = await _context.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(b => 
                (b.Domain != null && b.Domain.ToLower() == host.ToLower()) ||
                (b.AdminDomain != null && b.AdminDomain.ToLower() == host.ToLower()));

        return brand != null ? await GetBrandResponseAsync(brand) : null;
    }

    public async Task<IEnumerable<GetBrandGameResult>> GetBrandCatalogAsync(Guid brandId, Guid? brandScope = null)
    {
        // Verify brand access
        await GetBrandAsync(brandId, brandScope);

        var games = await _context.BrandGames
            .Include(bg => bg.Game)
            .Where(bg => bg.BrandId == brandId)
            .AsNoTracking()
            .OrderBy(bg => bg.DisplayOrder)
            .ThenBy(bg => bg.Game.Name)
            .Select(bg => new GetBrandGameResult(
                bg.GameId,
                bg.Game.Code,
                bg.Game.Name,
                bg.Game.Provider,
                bg.Enabled,
                bg.DisplayOrder,
                bg.Tags))
            .ToListAsync();

        return games;
    }

    public async Task InvalidateBrandCacheAsync(Guid brandId)
    {
        // Here you would invalidate Redis cache, in-memory cache, etc.
        // For now, just log the action
        _logger.LogInformation("Invalidating cache for brand: {BrandId}", brandId);
        await Task.CompletedTask;
    }

    // Private helper methods
    private async Task<Brand> GetBrandForUpdateAsync(Guid brandId, Guid? brandScope)
    {
        var query = _context.Brands.Where(b => b.Id == brandId);

        if (brandScope.HasValue)
            query = query.Where(b => b.Id == brandScope.Value);

        var brand = await query.FirstOrDefaultAsync();
        if (brand == null)
            throw new InvalidOperationException($"Brand {brandId} not found or access denied");

        return brand;
    }

    private async Task<GetBrandResponse> GetBrandResponseAsync(Brand brand)
    {
        return new GetBrandResponse(
            brand.Id,
            null, // OperatorId removed
            brand.Code,
            brand.Name,
            brand.Locale,
            brand.Domain,
            brand.AdminDomain,
            brand.CorsOrigins,
            brand.Theme,
            brand.Settings,
            brand.Status,
            brand.CreatedAt,
            brand.UpdatedAt,
            null); // Operator removed
    }

    private async Task ValidateUniqueDomainAsync(string domain, Guid excludeBrandId)
    {
        var exists = await _context.Brands.AsNoTracking()
            .AnyAsync(b => b.Domain == domain && b.Id != excludeBrandId);
        if (exists)
            throw new InvalidOperationException($"Domain '{domain}' is already in use");
    }

    private async Task ValidateUniqueAdminDomainAsync(string adminDomain, Guid excludeBrandId)
    {
        var exists = await _context.Brands.AsNoTracking()
            .AnyAsync(b => b.AdminDomain == adminDomain && b.Id != excludeBrandId);
        if (exists)
            throw new InvalidOperationException($"Admin domain '{adminDomain}' is already in use");
    }

    private static string GenerateSecretKey(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static string HashSecret(string secret)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hash)[..16]; // First 16 chars for logging
    }
}