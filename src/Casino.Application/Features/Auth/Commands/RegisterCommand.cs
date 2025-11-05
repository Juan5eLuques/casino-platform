using Casino.Application.Features.Auth;
using MediatR;

namespace Casino.Application.Features.Auth.Commands;

public record RegisterCommand(string Email, string Password, string? Role, string? Username = null) : IRequest<AuthResponse>;
