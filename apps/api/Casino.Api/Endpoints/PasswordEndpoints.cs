using Casino.Application.DTOs.Admin;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class PasswordEndpoints
{
    public static void MapPasswordEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/{userId:guid}/password", ChangePassword)
            .WithName("ChangePassword")
            .WithSummary("Change user password")
            .WithTags("Password Management")
            .Produces<PasswordChangeResponse>()
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .ProducesValidationProblem();

        app.MapPost("/users/{userId:guid}/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Reset user password (admin only)")
            .WithTags("Password Management")
            .Produces<PasswordChangeResponse>()
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> ChangePassword(
        Guid userId,
        [FromBody] ChangePasswordRequest request,
        IPasswordService passwordService,
        IValidator<ChangePasswordRequest> validator,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = GetCurrentUserId(httpContext);
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            // Validar permisos: usuario puede cambiar su propia password, o admin puede cambiar la de otros
            if (currentUserId != userId && currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER can only change their own password",
                    statusCode: 403);
            }

            var response = await passwordService.ChangeUserPasswordAsync(userId, request, currentUserId, operatorScope);
            
            if (!response.Success)
            {
                return Results.Problem(
                    title: "Password Change Failed",
                    detail: response.Message,
                    statusCode: 400);
            }

            logger.LogInformation("Password changed for user {UserId} by user {CurrentUserId}", userId, currentUserId);
            
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while changing password",
                statusCode: 500);
        }
    }

    private static async Task<IResult> ResetPassword(
        Guid userId,
        [FromBody] ResetPasswordRequest request,
        IPasswordService passwordService,
        IValidator<ResetPasswordRequest> validator,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        try
        {
            var currentUserId = GetCurrentUserId(httpContext);
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            // Solo SUPER_ADMIN y OPERATOR_ADMIN pueden resetear passwords
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot reset passwords",
                    statusCode: 403);
            }

            var response = await passwordService.ResetUserPasswordAsync(userId, request, currentUserId, operatorScope);
            
            if (!response.Success)
            {
                return Results.Problem(
                    title: "Password Reset Failed",
                    detail: response.Message,
                    statusCode: 400);
            }

            logger.LogInformation("Password reset for user {UserId} by user {CurrentUserId}", userId, currentUserId);
            
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while resetting password",
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

    private static Guid? GetOperatorScope(HttpContext httpContext, BackofficeUserRole role)
    {
        if (role == BackofficeUserRole.SUPER_ADMIN)
            return null; // SUPER_ADMIN ve todo

        var operatorIdClaim = httpContext.User.FindFirst("operator_id")?.Value;
        if (Guid.TryParse(operatorIdClaim, out var operatorId))
            return operatorId;

        return null;
    }
}