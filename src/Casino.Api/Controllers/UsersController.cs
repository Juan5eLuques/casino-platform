using Casino.Application.Features.Auth.Commands;
using Casino.Application.Features.Auth;
using Casino.Application.Features.Users;
using Casino.Application.Features.Users.Commands;
using Casino.Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casino.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Registro público - Solo permite crear jugadores (registro abierto)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterPlayer([FromBody] RegisterPlayerRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Email, request.Password, null, request.Username));
        return Ok(result);
    }

    /// <summary>
    /// Crear un nuevo usuario (Admin puede crear Admin/Cashier/Player, Cashier puede crear Cashier/Player)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SUPERADMIN,ADMIN,CASHIER")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _mediator.Send(new CreateUserCommand(
            request.Username,
            request.Email, 
            request.Password, 
            request.Role,
            request.CommissionRate
        ));
        return Ok(result);
    }

    /// <summary>
    /// Obtener usuarios del sistema con filtros, búsqueda y paginación (Solo para Admins y SuperAdmins)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SUPERADMIN,ADMIN")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string orderBy = "CreatedAt",
        [FromQuery] string orderByDirection = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int perPage = 10)
    {
        // Validar perPage
        if (perPage > 100) perPage = 100;
        if (perPage < 1) perPage = 10;
        
        // Validar page
        if (page < 1) page = 1;

        var result = await _mediator.Send(new GetAllUsersQuery(
            search, 
            role, 
            orderBy, 
            orderByDirection, 
            page, 
            perPage
        ));
        return Ok(result);
    }

    /// <summary>
    /// Obtener todos los usuarios creados directamente por el usuario actual
    /// </summary>
    [HttpGet("my-users")]
    [Authorize]
    public async Task<IActionResult> GetMyUsers()
    {
        var result = await _mediator.Send(new GetMyUsersQuery());
        return Ok(result);
    }

    /// <summary>
    /// Obtener la jerarquía completa del usuario actual y todos sus subordinados
    /// </summary>
    [HttpGet("hierarchy")]
    [Authorize]
    public async Task<IActionResult> GetHierarchy()
    {
        var result = await _mediator.Send(new GetUserHierarchyQuery());
        return Ok(result);
    }
}