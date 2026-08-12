using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Especialidades;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EspecialidadesController : ControllerBase
{
    private readonly IEspecialidadService _especialidadService;

    public EspecialidadesController(IEspecialidadService especialidadService)
    {
        _especialidadService = especialidadService;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas([FromQuery] string? buscar)
    {
        var especialidades = await _especialidadService.ObtenerTodosAsync(buscar);
        return Ok(especialidades);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var especialidad = await _especialidadService.ObtenerPorIdAsync(id);
        if (especialidad == null) return NotFound(new { mensaje = "Especialidad no encontrada." });
        return Ok(especialidad);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearEspecialidadDto dto)
    {
        var especialidad = await _especialidadService.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = especialidad.Id }, especialidad);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarEspecialidadDto dto)
    {
        var especialidad = await _especialidadService.EditarAsync(id, dto);
        if (especialidad == null) return NotFound(new { mensaje = "Especialidad no encontrada." });
        return Ok(especialidad);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var resultado = await _especialidadService.EliminarAsync(id);
            if (!resultado) return NotFound(new { mensaje = "Especialidad no encontrada." });
            return Ok(new { mensaje = "Especialidad eliminada correctamente." });
        }
        catch (Exception)
        {
            return BadRequest(new { mensaje = "No se puede eliminar la especialidad porque tiene registros asociados." });
        }
    }
}
