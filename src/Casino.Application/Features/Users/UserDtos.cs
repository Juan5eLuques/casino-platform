namespace Casino.Application.Features.Users;

public record UserResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    decimal Balance,
    decimal CommissionRate,
    int? ParentUserId,
    string? ParentEmail,
    DateTime CreatedAt
);

public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string Role,
    decimal? CommissionRate = null
);

public record RegisterPlayerRequest(
    string Username,
    string Email,
    string Password
);

public record UserHierarchyResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    decimal Balance,
    decimal CommissionRate,
    List<UserHierarchyResponse> ChildUsers
);

public record GetUsersRequest(
    string? Search = null,
    string? Role = null,
    string OrderBy = "CreatedAt",
    string OrderByDirection = "desc",
    int Page = 1,
    int PerPage = 10
);

public record PagedUsersResponse(
    List<UserResponse> Users,
    int TotalCount,
    int CurrentPage,
    int PerPage,
    int TotalPages
);