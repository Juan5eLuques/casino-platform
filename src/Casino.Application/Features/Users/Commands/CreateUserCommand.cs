using Casino.Application.Features.Auth;
using MediatR;

namespace Casino.Application.Features.Users.Commands;

public record CreateUserCommand(
    string Username,
    string Email, 
    string Password, 
    string Role,
    decimal? CommissionRate = null
) : IRequest<UserResponse>;