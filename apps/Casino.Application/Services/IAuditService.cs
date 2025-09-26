using System.Text.Json;

namespace Casino.Application.Services;

public interface IAuditService
{
    Task LogBackofficeActionAsync(Guid userId, string action, string targetType, string targetId, object? metadata = null);
    Task LogProviderActionAsync(string provider, string action, string? sessionId = null, string? playerId = null, 
        string? roundId = null, string? externalRef = null, object? requestData = null, object? responseData = null, 
        int statusCode = 200);
}