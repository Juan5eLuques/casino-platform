using Casino.Application.Features.Auth;
using MediatR;

namespace Casino.Application.Features.Auth.Queries;

public record MeQuery : IRequest<MeResponse>;
