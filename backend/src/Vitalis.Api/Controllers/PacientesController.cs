using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Pacientes;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Medico + "," + Roles.Recepcionista + "," + Roles.Facturacion)]
public class PacientesController : ControllerBase
{
    private readonly IPacienteService _pacienteService;

    public PacientesController(IPacienteService pacienteService)
    {
        _pacienteService = pacienteService;
    }

    // GET: api/Pacientes
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PacienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PacienteDto>>> ObtenerTodos([FromQuery] string? buscar)
    {
        var pacientes = await _pacienteService.ObtenerTodosAsync(buscar);
        return Ok(pacientes);
    }

    // GET: api/Pacientes/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PacienteDto>> ObtenerPorId(int id)
    {
        var paciente = await _pacienteService.ObtenerPorIdAsync(id);
        if (paciente == null) return NotFound(new { mensaje = "Paciente no encontrado." });
        return Ok(paciente);
    }

    // POST: api/Pacientes
    [Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista)]
    [HttpPost]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PacienteDto>> Crear([FromBody] CrearPacienteDto dto)
    {
        var paciente = await _pacienteService.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = paciente.Id }, paciente);
    }

    // PUT: api/Pacientes/{id}
    [Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista)]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PacienteDto>> Editar(int id, [FromBody] EditarPacienteDto dto)
    {
        var paciente = await _pacienteService.EditarAsync(id, dto);
        if (paciente == null) return NotFound(new { mensaje = "Paciente no encontrado." });
        return Ok(paciente);
    }

    // DELETE: api/Pacientes/{id}
    [Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var resultado = await _pacienteService.DesactivarAsync(id);
        if (!resultado) return NotFound(new { mensaje = "Paciente no encontrado." });
        return Ok(new { mensaje = "Paciente desactivado correctamente." });
    }
}
