using Casino.Application.Features.Users;
using MediatR;

namespace Casino.Application.Features.Users.Queries;

public record GetMyUsersQuery() : IRequest<List<UserResponse>>;

public record GetUserHierarchyQuery() : IRequest<UserHierarchyResponse>;