using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Prescripciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Medico)]
public class PrescripcionesController : ControllerBase
{
    private readonly IPrescripcionService _service;
    public PrescripcionesController(IPrescripcionService service) => _service = service;

    [HttpGet("paciente/{pacienteId}")]
    public async Task<IActionResult> GetByPaciente(int pacienteId)
    {
        return Ok(await _service.ObtenerPorPacienteAsync(pacienteId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var presc = await _service.ObtenerPorIdAsync(id);
        if (presc == null) return NotFound();
        return Ok(presc);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearPrescripcionDto dto)
    {
        var presc = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = presc.Id }, presc);
    }
}
