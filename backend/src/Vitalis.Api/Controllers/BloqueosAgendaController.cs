using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Bloqueos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista + "," + Roles.Medico)]
public class BloqueosAgendaController : ControllerBase
{
    private readonly IBloqueoAgendaService _bloqueoService;

    public BloqueosAgendaController(IBloqueoAgendaService bloqueoService)
    {
        _bloqueoService = bloqueoService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var bloqueos = await _bloqueoService.ObtenerTodosAsync();
        return Ok(bloqueos);
    }

    [HttpGet("profesional/{profesionalId}")]
    public async Task<IActionResult> GetByProfesional(int profesionalId)
    {
        var bloqueos = await _bloqueoService.ObtenerPorProfesionalAsync(profesionalId);
        return Ok(bloqueos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var bloqueo = await _bloqueoService.ObtenerPorIdAsync(id);
        if (bloqueo == null) return NotFound();
        return Ok(bloqueo);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearBloqueoDto dto)
    {
        var bloqueo = await _bloqueoService.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = bloqueo.Id }, bloqueo);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _bloqueoService.EliminarAsync(id);
        if (!eliminado) return NotFound();
        return NoContent();
    }
}
