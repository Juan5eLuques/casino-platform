using Casino.Application.DTOs.Admin;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class BackofficeUserEndpoints
{
    public static void MapBackofficeUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users", CreateBackofficeUser)
            .WithName("CreateBackofficeUser")
            .WithSummary("Create a new backoffice user")
            .WithTags("Backoffice User Management")
            .Produces<GetBackofficeUserResponse>(201)
            .Produces(400)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapGet("/users", GetBackofficeUsers)
            .WithName("GetBackofficeUsers")
            .WithSummary("Get backoffice users with filtering and pagination")
            .WithTags("Backoffice User Management")
            .Produces<QueryBackofficeUsersResponse>();

        app.MapGet("/users/{userId:guid}", GetBackofficeUser)
            .WithName("GetBackofficeUser")
            .WithSummary("Get backoffice user by ID")
            .WithTags("Backoffice User Management")
            .Produces<GetBackofficeUserResponse>()
            .Produces(404);

        app.MapPatch("/users/{userId:guid}", UpdateBackofficeUser)
            .WithName("UpdateBackofficeUser")
            .WithSummary("Update backoffice user")
            .WithTags("Backoffice User Management")
            .Produces<GetBackofficeUserResponse>()
            .Produces(404)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapDelete("/users/{userId:guid}", DeleteBackofficeUser)
            .WithName("DeleteBackofficeUser")
            .WithSummary("Delete backoffice user")
            .WithTags("Backoffice User Management")
            .Produces(200)
            .Produces(404)
            .Produces(409);
    }

    private static async Task<IResult> CreateBackofficeUser(
        [FromBody] CreateBackofficeUserRequest request,
        IBackofficeUserService backofficeUserService,
        IValidator<CreateBackofficeUserRequest> validator,
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
            var currentOperatorId = GetCurrentOperatorId(httpContext);

            // Validaciones de autorización
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                // OPERATOR_ADMIN solo puede crear usuarios en su propio operador
                if (request.Role == BackofficeUserRole.SUPER_ADMIN)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "Only SUPER_ADMIN can create SUPER_ADMIN users",
                        statusCode: 403);
                }

                if (request.OperatorId != currentOperatorId)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "Can only create users for your own operator",
                        statusCode: 403);
                }

                // CASHIER no puede crear usuarios
                if (currentRole == BackofficeUserRole.CASHIER)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "CASHIER role cannot create users",
                        statusCode: 403);
                }
            }

            var response = await backofficeUserService.CreateUserAsync(request, currentUserId);
            
            logger.LogInformation("Backoffice user created: {UserId} - {Username} - {Role} by user {CreatedByUserId}",
                response.Id, response.Username, response.Role, currentUserId);
            
            return TypedResults.Created($"/api/v1/admin/users/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Backoffice user creation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "User Creation Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating backoffice user with username: {Username}", request.Username);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating user",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBackofficeUsers(
        [AsParameters] QueryBackofficeUsersRequest request,
        IBackofficeUserService backofficeUserService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            var response = await backofficeUserService.GetUsersAsync(request, operatorScope);
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting backoffice users");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting users",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetBackofficeUser(
        Guid userId,
        IBackofficeUserService backofficeUserService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            var response = await backofficeUserService.GetUserAsync(userId, operatorScope);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "User Not Found",
                    detail: "User does not exist or access denied",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting backoffice user: {UserId}", userId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting user",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateBackofficeUser(
        Guid userId,
        [FromBody] UpdateBackofficeUserRequest request,
        IBackofficeUserService backofficeUserService,
        IValidator<UpdateBackofficeUserRequest> validator,
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

            // Validaciones de autorización
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                if (request.Role == BackofficeUserRole.SUPER_ADMIN)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "Only SUPER_ADMIN can manage SUPER_ADMIN users",
                        statusCode: 403);
                }

                if (currentRole == BackofficeUserRole.CASHIER)
                {
                    return Results.Problem(
                        title: "Access Denied",
                        detail: "CASHIER role cannot update users",
                        statusCode: 403);
                }
            }

            var response = await backofficeUserService.UpdateUserAsync(userId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Backoffice user updated: {UserId} by user {UpdatedByUserId}",
                userId, currentUserId);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Backoffice user update failed: {Message}", ex.Message);
            return Results.Problem(
                title: "User Update Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating backoffice user: {UserId}", userId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating user",
                statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteBackofficeUser(
        Guid userId,
        IBackofficeUserService backofficeUserService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentUserId = GetCurrentUserId(httpContext);
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            // Validaciones de autorización
            if (currentRole == BackofficeUserRole.CASHIER)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "CASHIER role cannot delete users",
                    statusCode: 403);
            }

            var success = await backofficeUserService.DeleteUserAsync(userId, currentUserId, operatorScope);
            
            if (!success)
            {
                return Results.Problem(
                    title: "User Not Found",
                    detail: "User does not exist or access denied",
                    statusCode: 404);
            }

            logger.LogInformation("Backoffice user deleted: {UserId} by user {DeletedByUserId}",
                userId, currentUserId);
            
            return TypedResults.Ok(new { Success = true, Message = "User deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Backoffice user deletion failed: {Message}", ex.Message);
            return Results.Problem(
                title: "User Deletion Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting backoffice user: {UserId}", userId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while deleting user",
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

    private static Guid? GetCurrentOperatorId(HttpContext httpContext)
    {
        var operatorIdClaim = httpContext.User.FindFirst("operator_id")?.Value;
        if (Guid.TryParse(operatorIdClaim, out var operatorId))
            return operatorId;
        return null;
    }

    private static Guid? GetOperatorScope(HttpContext httpContext, BackofficeUserRole role)
    {
        if (role == BackofficeUserRole.SUPER_ADMIN)
            return null; // SUPER_ADMIN ve todos los usuarios

        return GetCurrentOperatorId(httpContext);
    }
}