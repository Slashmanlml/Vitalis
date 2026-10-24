using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.Interfaces;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    IRegistroPacienteService registroService,
    IRecuperacionPasswordService recuperacionService) : ControllerBase
{
    /// <summary>Políticas de límite de pedidos (ver Program.cs).</summary>
    public const string PoliticaLogin = "login";
    public const string PoliticaRegistro = "registro";

    [AllowAnonymous]
    [EnableRateLimiting(PoliticaLogin)]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (result is null)
        {
            return Unauthorized(new { mensaje = "Email o contraseña incorrectos.", message = "Email o contraseña incorrectos." });
        }

        return Ok(result);
    }

    /// <summary>Alta de un paciente desde la web pública. La cuenta queda pendiente de confirmar el correo.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaRegistro)]
    [HttpPost("registro")]
    [ProducesResponseType(typeof(MensajeDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MensajeDto>> Registro([FromBody] RegistroPacienteDto dto, CancellationToken ct)
    {
        // 202 y no 201: no se informa si se creó algo (ver respuesta genérica
        // en RegistroPacienteService).
        return Accepted(await registroService.RegistrarAsync(dto, ct));
    }

    /// <summary>
    /// Confirma el correo con el token del enlace. Es POST (lo llama la página
    /// /confirmar-email del frontend) y no un GET directo desde el mail: hay
    /// antivirus y clientes de correo que abren los enlaces solos para
    /// revisarlos, y con un GET confirmarían la cuenta sin que la persona haga
    /// nada.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaRegistro)]
    [HttpPost("confirmar-email")]
    public async Task<ActionResult<MensajeDto>> ConfirmarEmail([FromBody] ConfirmarEmailDto dto, CancellationToken ct) =>
        Ok(await registroService.ConfirmarEmailAsync(dto.Token, ct));

    [AllowAnonymous]
    [EnableRateLimiting(PoliticaRegistro)]
    [HttpPost("reenviar-confirmacion")]
    public async Task<ActionResult<MensajeDto>> ReenviarConfirmacion([FromBody] ReenviarConfirmacionDto dto, CancellationToken ct) =>
        Accepted(await registroService.ReenviarConfirmacionAsync(dto.Email, ct));

    /// <summary>"Olvidé mi contraseña": manda un enlace por correo. Respuesta siempre igual.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaRegistro)]
    [HttpPost("olvide-password")]
    public async Task<ActionResult<MensajeDto>> OlvidePassword([FromBody] SolicitarRecuperacionDto dto, CancellationToken ct) =>
        Accepted(await recuperacionService.SolicitarAsync(dto.Email, ct));

    /// <summary>Elige una contraseña nueva con el token del enlace.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaRegistro)]
    [HttpPost("restablecer-password")]
    public async Task<ActionResult<MensajeDto>> RestablecerPassword([FromBody] RestablecerPasswordDto dto, CancellationToken ct) =>
        Ok(await recuperacionService.RestablecerAsync(dto, ct));

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
            return BadRequest(new { message = "Contraseña actual incorrecta.", mensaje = "Contraseña actual incorrecta." });
        return Ok(new { message = "Contraseña actualizada correctamente.", mensaje = "Contraseña actualizada correctamente." });
    }

    private int ObtenerUsuarioId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return int.Parse(claim?.Value ?? "0");
    }
}
