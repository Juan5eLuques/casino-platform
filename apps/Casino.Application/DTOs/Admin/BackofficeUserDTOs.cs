using Casino.Domain.Enums;

namespace Casino.Application.DTOs.Admin;

// Request DTOs
public record CreateBackofficeUserRequest(
    string Username,
    string Password,
    BackofficeUserRole Role,
    Guid? OperatorId = null); // null para SUPER_ADMIN

public record UpdateBackofficeUserRequest(
    string? Username = null,
    string? Password = null,
    BackofficeUserRole? Role = null,
    BackofficeUserStatus? Status = null,
    Guid? OperatorId = null);

public record QueryBackofficeUsersRequest(
    string? Username = null,
    BackofficeUserRole? Role = null,
    BackofficeUserStatus? Status = null,
    Guid? OperatorId = null,
    int Page = 1,
    int PageSize = 20);

// Response DTOs
public record GetBackofficeUserResponse(
    Guid Id,
    string Username,
    BackofficeUserRole Role,
    BackofficeUserStatus Status,
    Guid? OperatorId,
    string? OperatorName,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record QueryBackofficeUsersResponse(
    IEnumerable<GetBackofficeUserResponse> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);