using Casino.Application.Services;
using Casino.Domain.Entities;
using Casino.Infrastructure.Data;
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
}