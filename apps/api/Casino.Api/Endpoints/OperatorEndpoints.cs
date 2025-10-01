using Casino.Application.DTOs.Operator;
using Casino.Application.Services;
using Casino.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Endpoints;

public static class OperatorEndpoints
{
    public static void MapOperatorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/operators", CreateOperator)
            .WithName("CreateOperator")
            .WithSummary("Create a new operator")
            .WithTags("Operator Management")
            .Produces<GetOperatorResponse>(201)
            .Produces(400)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapGet("/operators", GetOperators)
            .WithName("GetOperators")
            .WithSummary("Get operators with filtering and pagination")
            .WithTags("Operator Management")
            .Produces<QueryOperatorsResponse>();

        app.MapGet("/operators/{operatorId:guid}", GetOperator)
            .WithName("GetOperator")
            .WithSummary("Get operator by ID")
            .WithTags("Operator Management")
            .Produces<GetOperatorResponse>()
            .Produces(404);

        app.MapPatch("/operators/{operatorId:guid}", UpdateOperator)
            .WithName("UpdateOperator")
            .WithSummary("Update operator")
            .WithTags("Operator Management")
            .Produces<GetOperatorResponse>()
            .Produces(404)
            .Produces(409)
            .ProducesValidationProblem();

        app.MapDelete("/operators/{operatorId:guid}", DeleteOperator)
            .WithName("DeleteOperator")
            .WithSummary("Delete operator")
            .WithTags("Operator Management")
            .Produces(200)
            .Produces(404)
            .Produces(409);
    }

    private static async Task<IResult> CreateOperator(
        [FromBody] CreateOperatorRequest request,
        IOperatorService operatorService,
        IValidator<CreateOperatorRequest> validator,
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

            // Solo SUPER_ADMIN puede crear operadores
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN can create operators",
                    statusCode: 403);
            }

            var response = await operatorService.CreateOperatorAsync(request, currentUserId);
            
            logger.LogInformation("Operator created: {OperatorId} - {Name} by user {UserId}",
                response.Id, response.Name, currentUserId);
            
            return TypedResults.Created($"/api/v1/admin/operators/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Operator creation failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Operator Creation Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating operator with name: {Name}", request.Name);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while creating operator",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetOperators(
        [AsParameters] QueryOperatorsRequest request,
        IOperatorService operatorService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            var response = await operatorService.GetOperatorsAsync(request, operatorScope);
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting operators");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting operators",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetOperator(
        Guid operatorId,
        IOperatorService operatorService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentRole = GetCurrentUserRole(httpContext);
            var operatorScope = GetOperatorScope(httpContext, currentRole);

            var response = await operatorService.GetOperatorAsync(operatorId, operatorScope);
            
            if (response == null)
            {
                return Results.Problem(
                    title: "Operator Not Found",
                    detail: "Operator does not exist or access denied",
                    statusCode: 404);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting operator: {OperatorId}", operatorId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while getting operator",
                statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateOperator(
        Guid operatorId,
        [FromBody] UpdateOperatorRequest request,
        IOperatorService operatorService,
        IValidator<UpdateOperatorRequest> validator,
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

            var response = await operatorService.UpdateOperatorAsync(operatorId, request, currentUserId, operatorScope);
            
            logger.LogInformation("Operator updated: {OperatorId} by user {UserId}",
                operatorId, currentUserId);
            
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Operator update failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Operator Update Failed",
                detail: ex.Message,
                statusCode: ex.Message.Contains("not found") ? 404 : 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating operator: {OperatorId}", operatorId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while updating operator",
                statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteOperator(
        Guid operatorId,
        IOperatorService operatorService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            var currentUserId = GetCurrentUserId(httpContext);
            var currentRole = GetCurrentUserRole(httpContext);

            // Solo SUPER_ADMIN puede eliminar operadores
            if (currentRole != BackofficeUserRole.SUPER_ADMIN)
            {
                return Results.Problem(
                    title: "Access Denied",
                    detail: "Only SUPER_ADMIN can delete operators",
                    statusCode: 403);
            }

            var success = await operatorService.DeleteOperatorAsync(operatorId, currentUserId, null);
            
            if (!success)
            {
                return Results.Problem(
                    title: "Operator Not Found",
                    detail: "Operator does not exist",
                    statusCode: 404);
            }

            logger.LogInformation("Operator deleted: {OperatorId} by user {UserId}",
                operatorId, currentUserId);
            
            return TypedResults.Ok(new { Success = true, Message = "Operator deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Operator deletion failed: {Message}", ex.Message);
            return Results.Problem(
                title: "Operator Deletion Failed",
                detail: ex.Message,
                statusCode: 409);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting operator: {OperatorId}", operatorId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while deleting operator",
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
            return null; // SUPER_ADMIN ve todos los operadores

        var operatorIdClaim = httpContext.User.FindFirst("operator_id")?.Value;
        if (Guid.TryParse(operatorIdClaim, out var operatorId))
            return operatorId;

        return null;
    }
}