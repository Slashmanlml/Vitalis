using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Consultas;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Solo el rol Medico. El administrador es una figura tecnica y administrativa,
// no asistencial: gestiona usuarios, agenda y facturacion, pero el contenido
// clinico (diagnosticos, evolucion, indicaciones, recetas) es materia de secreto
// profesional y no le corresponde. Antes tenia acceso completo.
[Authorize(Roles = Roles.Medico)]
public class ConsultasMedicasController : ControllerBase
{
    private readonly IConsultaMedicaService _service;
    public ConsultasMedicasController(IConsultaMedicaService service) => _service = service;

    [HttpGet("paciente/{pacienteId}")]
    public async Task<IActionResult> GetByPaciente(int pacienteId)
    {
        var consultas = await _service.ObtenerPorPacienteAsync(pacienteId);
        return Ok(consultas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await _service.ObtenerPorIdAsync(id);
        if (consulta == null) return NotFound();
        return Ok(consulta);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearConsultaDto dto)
    {
        var consulta = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, consulta);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] EditarConsultaDto dto)
    {
        var consulta = await _service.EditarAsync(id, dto);
        if (consulta == null) return NotFound();
        return Ok(consulta);
    }

    [HttpGet("antecedentes/{pacienteId}")]
    public async Task<IActionResult> GetAntecedentes(int pacienteId)
    {
        return Ok(await _service.ObtenerAntecedentesAsync(pacienteId));
    }

    [HttpPost("antecedentes")]
    public async Task<IActionResult> PostAntecedente([FromBody] CrearAntecedenteDto dto)
    {
        var ant = await _service.CrearAntecedenteAsync(dto);
        return CreatedAtAction(nameof(GetAntecedentes), new { pacienteId = dto.PacienteId }, ant);
    }

    [HttpGet("alergias/{pacienteId}")]
    public async Task<IActionResult> GetAlergias(int pacienteId)
    {
        return Ok(await _service.ObtenerAlergiasAsync(pacienteId));
    }

    [HttpPost("alergias")]
    public async Task<IActionResult> PostAlergia([FromBody] CrearAlergiaDto dto)
    {
        var al = await _service.CrearAlergiaAsync(dto);
        return CreatedAtAction(nameof(GetAlergias), new { pacienteId = dto.PacienteId }, al);
    }
}
