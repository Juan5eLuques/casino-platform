using Casino.Application.DTOs.Audit;
using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Casino.Application.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly CasinoDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(CasinoDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogBackofficeActionAsync(Guid userId, string action, string targetType, string targetId, object? metadata = null)
    {
        try
        {
            var auditEntry = new BackofficeAudit
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Meta = metadata != null ? JsonDocument.Parse(JsonSerializer.Serialize(metadata)) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.BackofficeAudits.Add(auditEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Backoffice action logged: {Action} on {TargetType}:{TargetId} by user {UserId}", 
                action, targetType, targetId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging backoffice action: {Action} by user {UserId}", action, userId);
            // Don't throw - audit logging should not break business operations
        }
    }

    public async Task LogProviderActionAsync(string provider, string action, string? sessionId = null, string? playerId = null, 
        string? roundId = null, string? externalRef = null, object? requestData = null, object? responseData = null, 
        int statusCode = 200)
    {
        try
        {
            var auditEntry = new ProviderAudit
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                Action = action,
                SessionId = sessionId,
                PlayerId = playerId,
                RoundId = roundId,
                ExternalRef = externalRef,
                RequestData = requestData != null ? JsonDocument.Parse(JsonSerializer.Serialize(requestData)) : null,
                ResponseData = responseData != null ? JsonDocument.Parse(JsonSerializer.Serialize(responseData)) : null,
                StatusCode = statusCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProviderAudits.Add(auditEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Provider action logged: {Action} by {Provider} for session {SessionId}", 
                action, provider, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging provider action: {Action} by provider {Provider}", action, provider);
            // Don't throw - audit logging should not break business operations
        }
    }

    public async Task<QueryBackofficeAuditResponse> GetBackofficeAuditAsync(QueryBackofficeAuditRequest request)
    {
        var query = _context.BackofficeAudits
            .Include(a => a.User)
            .AsNoTracking();

        // Apply filters
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(a => a.Action.Contains(request.Action));

        if (!string.IsNullOrEmpty(request.TargetType))
            query = query.Where(a => a.TargetType == request.TargetType);

        if (request.TargetId.HasValue)
            query = query.Where(a => a.TargetId == request.TargetId.Value.ToString());

        if (request.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= request.ToDate.Value);

        // Apply brand scope if provided
        if (request.BrandId.HasValue)
            query = query.Where(a => a.User.BrandId == request.BrandId.Value);

        // Count total
        var totalCount = await query.CountAsync();

        // Apply pagination and get raw data first
        var rawAudits = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Process in memory to avoid Expression Tree issues
        var audits = rawAudits.Select(a => new BackofficeAuditResponse(
            a.Id,
            a.UserId,
            a.User.Username,
            a.User.Role.ToString(),
            null, // Operator name removed
            a.Action,
            a.TargetType,
            a.TargetId,
            a.Meta != null ? JsonSerializer.Deserialize<object>(a.Meta.RootElement.GetRawText()) : null,
            a.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new QueryBackofficeAuditResponse(audits, request.Page, request.PageSize, totalCount, totalPages);
    }

    public async Task<QueryProviderAuditResponse> GetProviderAuditAsync(QueryProviderAuditRequest request)
    {
        var query = _context.ProviderAudits.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Provider))
            query = query.Where(a => a.Provider == request.Provider);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(a => a.Action.Contains(request.Action));

        if (!string.IsNullOrEmpty(request.SessionId))
            query = query.Where(a => a.SessionId == request.SessionId);

        if (!string.IsNullOrEmpty(request.PlayerId))
            query = query.Where(a => a.PlayerId == request.PlayerId);

        if (!string.IsNullOrEmpty(request.GameCode))
        {
            // Search GameCode within RequestData or ResponseData JSON
            query = query.Where(a => 
                (a.RequestData != null && a.RequestData.RootElement.GetRawText().Contains(request.GameCode)) ||
                (a.ResponseData != null && a.ResponseData.RootElement.GetRawText().Contains(request.GameCode)));
        }

        if (!string.IsNullOrEmpty(request.RoundId))
            query = query.Where(a => a.RoundId == request.RoundId);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= request.ToDate.Value);

        // Apply brand scope if provided (joining with players to get brand info)
        if (request.BrandId.HasValue)
        {
            query = query.Where(a => a.PlayerId != null &&
                _context.Players.Any(p => p.Id == Guid.Parse(a.PlayerId) && p.BrandId == request.BrandId.Value));
        }

        // Count total
        var totalCount = await query.CountAsync();

        // Apply pagination and get data using ToListAsync first
        var rawAudits = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // Process in memory to avoid Expression Tree issues
        var audits = rawAudits.Select(a => new ProviderAuditResponse(
            a.Id,
            a.Provider,
            a.Action,
            a.SessionId,
            a.PlayerId,
            a.RoundId,
            ExtractGameCodeFromJson(a.RequestData, a.ResponseData),
            a.ExternalRef,
            a.RequestData != null ? JsonSerializer.Deserialize<object>(a.RequestData.RootElement.GetRawText()) : null,
            a.ResponseData != null ? JsonSerializer.Deserialize<object>(a.ResponseData.RootElement.GetRawText()) : null,
            a.StatusCode,
            a.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new QueryProviderAuditResponse(audits, request.Page, request.PageSize, totalCount, totalPages);
    }

    private static string? ExtractGameCodeFromJson(JsonDocument? requestData, JsonDocument? responseData)
    {
        try
        {
            // Try to extract gameCode from request data
            if (requestData != null && requestData.RootElement.TryGetProperty("gameCode", out var gameCodeElement))
                return gameCodeElement.GetString();

            // Try to extract gameCode from response data
            if (responseData != null && responseData.RootElement.TryGetProperty("gameCode", out var responseGameCodeElement))
                return responseGameCodeElement.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }
}