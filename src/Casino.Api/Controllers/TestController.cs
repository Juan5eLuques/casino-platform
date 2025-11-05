using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Casino.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Endpoint público para pruebas
    /// </summary>
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok(new { message = "Endpoint público funcionando", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Endpoint que requiere autenticación
    /// </summary>
    [HttpGet("protected")]
    [Authorize]
    public IActionResult Protected()
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User?.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList();
        
        return Ok(new { 
            message = "Endpoint protegido funcionando",
            userId,
            email,
            roles,
            claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToList(),
            timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// Endpoint que requiere rol específico
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "ADMIN,SUPERADMIN")]
    public IActionResult AdminOnly()
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User?.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList();
        
        return Ok(new { 
            message = "Endpoint solo para admins funcionando",
            userId,
            email,
            roles,
            timestamp = DateTime.UtcNow 
        });
    }
}