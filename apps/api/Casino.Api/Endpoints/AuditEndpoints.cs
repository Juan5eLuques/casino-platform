using Casino.Application.DTOs.Audit;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        // Auditoría de backoffice
        app.MapGet("/audit/backoffice", GetBackofficeAudit)
            .WithName("GetBackofficeAudit")
            .WithSummary("Get backoffice audit logs with filtering and pagination")
            .WithTags("Audit")
            .Produces<QueryBackofficeAuditResponse>()
            .Produces(400)
            .Produces(403)
            .ProducesValidationProblem();

        // Auditoría de proveedores
        app.MapGet("/audit/provider", GetProviderAudit)
            .WithName("GetProviderAudit")
            .WithSummary("Get provider audit logs with filtering and pagination")
            .WithTags("Audit")
            .Produces<QueryProviderAuditResponse>()
            .Produces(400)
            .Produces(403)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetBackofficeAudit(
        [FromServices] IAuditService auditService,
        [FromServices] ILogger<Program> logger,
        HttpContext httpContext,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetType = null,
        [FromQuery] Guid? targetId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // SONNET: Basic validation without FluentValidation for now
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 50;

            var request = new QueryBackofficeAuditRequest
            {
                UserId = userId,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            };

            // Verificar permisos basados en el rol del usuario
            var currentRole = GetCurrentUserRole(httpContext);

            // Solo admins pueden ver auditoría, CASHIER no tiene acceso
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot access audit logs",
                    statusCode: 403);
            }

            // BRAND_ADMIN solo puede ver auditoría de su brand
            if (currentRole == BackofficeUserRole.BRAND_ADMIN)
            {
                var currentBrandId = GetCurrentBrandId(httpContext);
                if (currentBrandId.HasValue)
                {
                    request.BrandId = currentBrandId.Value;
                }
            }

            var response = await auditService.GetBackofficeAuditAsync(request);
            
            logger.LogInformation("Backoffice audit queried by user {UserId} - Role: {Role}, Records: {Count}",
                GetCurrentUserId(httpContext), currentRole, response.Data.Count);
            
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting backoffice audit");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting audit logs",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetProviderAudit(
        [FromServices] IAuditService auditService,
        [FromServices] ILogger<Program> logger,
        HttpContext httpContext,
        [FromQuery] string? provider = null,
        [FromQuery] string? action = null,
        [FromQuery] string? sessionId = null,
        [FromQuery] string? playerId = null,
        [FromQuery] string? gameCode = null,
        [FromQuery] string? roundId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // SONNET: Basic validation without FluentValidation for now
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 50;

            var request = new QueryProviderAuditRequest
            {
                Provider = provider,
                Action = action,
                SessionId = sessionId,
                PlayerId = playerId,
                GameCode = gameCode,
                RoundId = roundId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            };

            // Verificar permisos basados en el rol del usuario
            var currentRole = GetCurrentUserRole(httpContext);
            var currentBrandId = GetCurrentBrandId(httpContext);

            // Solo admins pueden ver auditoría, CASHIER no tiene acceso
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot access audit logs",
                    statusCode: 403);
            }

            // BRAND_ADMIN solo puede ver auditoría de su brand
            if (currentRole == BackofficeUserRole.BRAND_ADMIN && currentBrandId.HasValue)
            {
                request.BrandId = currentBrandId.Value;
            }

            var response = await auditService.GetProviderAuditAsync(request);
            
            logger.LogInformation("Provider audit queried by user {UserId} - Role: {Role}, Records: {Count}",
                GetCurrentUserId(httpContext), currentRole, response.Data.Count);
            
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting provider audit");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting audit logs",
                statusCode: 500);
        }
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("Invalid user ID in token");
        }
        return userId;
    }

    private static BackofficeUserRole GetCurrentUserRole(HttpContext httpContext)
    {
        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<BackofficeUserRole>(roleClaim, out var role))
        {
            throw new InvalidOperationException("Invalid role in token");
        }
        return role;
    }

    private static Guid? GetCurrentBrandId(HttpContext httpContext)
    {
        var brandIdClaim = httpContext.User.FindFirst("brand_id")?.Value;
        if (Guid.TryParse(brandIdClaim, out var brandId))
            return brandId;
        return null;
    }
}