using Casino.Application.Features.Auth;
using MediatR;

namespace Casino.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
