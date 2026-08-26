using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador)]
public class EmailLogsController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailLogsController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? origen = null, 
        [FromQuery] string? evento = null, 
        [FromQuery] string? estado = null)
    {
        var logs = await _emailService.GetEmailLogsAsync(origen, evento, estado);
        return Ok(logs);
    }

    [HttpPost("simular")]
    public async Task<IActionResult> Simular([FromBody] SimularEmailDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var log = await _emailService.SimularEnvioAsync(
            dto.Destinatario,
            dto.TipoNotificacion,
            dto.Asunto,
            dto.Cuerpo
        );

        return Ok(log);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _emailService.EliminarLogAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
