using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Turnos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista + "," + Roles.Medico)]
public class TurnosController : ControllerBase
{
    private readonly ITurnoService _turnoService;

    public TurnosController(ITurnoService turnoService)
    {
        _turnoService = turnoService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var turnos = await _turnoService.ObtenerTodosAsync();
        return Ok(turnos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var turno = await _turnoService.ObtenerPorIdAsync(id);
        if (turno == null) return NotFound();
        return Ok(turno);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearTurnoDto dto)
    {
        var turno = await _turnoService.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = turno.Id }, turno);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] EditarTurnoDto dto)
    {
        var turno = await _turnoService.EditarAsync(id, dto);
        if (turno == null) return NotFound();
        return Ok(turno);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _turnoService.EliminarAsync(id);
        if (!eliminado) return NotFound();
        return NoContent();
    }
}
