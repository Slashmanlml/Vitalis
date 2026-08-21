using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador)]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet("TurnosPorProfesional/{profesionalId}")]
    public async Task<IActionResult> TurnosPorProfesional(int profesionalId, DateTime? desde, DateTime? hasta)
    {
        var turnos = await _reporteService.TurnosPorProfesionalAsync(profesionalId, desde, hasta);
        return Ok(turnos);
    }

    [HttpGet("TurnosPorPaciente/{pacienteId}")]
    public async Task<IActionResult> TurnosPorPaciente(int pacienteId)
    {
        var turnos = await _reporteService.TurnosPorPacienteAsync(pacienteId);
        return Ok(turnos);
    }

    [HttpGet("TurnosPorObraSocial/{obraSocialId}")]
    public async Task<IActionResult> TurnosPorObraSocial(int obraSocialId)
    {
        var turnos = await _reporteService.TurnosPorObraSocialAsync(obraSocialId);
        return Ok(turnos);
    }

    [HttpGet("Estadisticas")]
    public async Task<IActionResult> Estadisticas()
    {
        var estadisticas = await _reporteService.EstadisticasGeneralesAsync();
        return Ok(estadisticas);
    }
}
