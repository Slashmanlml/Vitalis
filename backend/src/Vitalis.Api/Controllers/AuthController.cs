using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.Interfaces;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (result is null)
        {
            return Unauthorized(new { message = "Email o contraseña incorrectos." });
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioPerfilDto>> GetProfile()
    {
        var usuarioId = ObtenerUsuarioId();
        var perfil = await authService.ObtenerPerfilAsync(usuarioId);
        if (perfil == null) return NotFound();
        return Ok(perfil);
    }

    [Authorize]
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] CambiarPasswordDto dto)
    {
        var usuarioId = ObtenerUsuarioId();
        var result = await authService.CambiarPasswordAsync(usuarioId, dto);
        if (!result)
            return BadRequest(new { message = "Contraseña actual incorrecta." });
        return Ok(new { message = "Contraseña actualizada correctamente." });
    }

    private int ObtenerUsuarioId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.Parse(claim?.Value ?? "0");
    }
}
