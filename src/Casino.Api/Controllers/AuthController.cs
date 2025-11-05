using Casino.Application.Features.Auth;
using Casino.Application.Features.Auth.Commands;
using Casino.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Login general para todos los usuarios
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest r)
    {
        var result = await _mediator.Send(new LoginCommand(r.Email, r.Password));
        return Ok(result);
    }

    /// <summary>
    /// Login específico para administradores (puede tener lógica adicional)
    /// </summary>
    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest r)
    {
        var result = await _mediator.Send(new AdminLoginCommand(r.Email, r.Password));
        return Ok(result);
    }

    /// <summary>
    /// Obtener información del usuario autenticado
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _mediator.Send(new MeQuery());
        return Ok(result);
    }
}
