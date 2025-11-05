using Casino.Application.Features.Users;
using MediatR;

namespace Casino.Application.Features.Users.Queries;

public record GetAllUsersQuery(
    string? Search = null,
    string? Role = null,
    string OrderBy = "CreatedAt",
    string OrderByDirection = "desc",
    int Page = 1,
    int PerPage = 10
) : IRequest<PagedUsersResponse>;