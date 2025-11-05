using Casino.Application.Features.Auth;
using MediatR;

namespace Casino.Application.Features.Auth.Commands;

public record AdminLoginCommand(string Email, string Password) : IRequest<AuthResponse>;
