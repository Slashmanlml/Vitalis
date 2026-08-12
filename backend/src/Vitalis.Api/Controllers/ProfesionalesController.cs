using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Profesionales;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfesionalesController : ControllerBase
{
    private readonly IProfesionalService _profesionalService;

    public ProfesionalesController(IProfesionalService profesionalService)
    {
        _profesionalService = profesionalService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var profesionales = await _profesionalService.ObtenerTodosAsync();
        return Ok(profesionales);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var profesional = await _profesionalService.ObtenerPorIdAsync(id);
        if (profesional == null) return NotFound();
        return Ok(profesional);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearProfesionalDto dto)
    {
        var profesional = await _profesionalService.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = profesional.Id }, profesional);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] EditarProfesionalDto dto)
    {
        var profesional = await _profesionalService.EditarAsync(id, dto);
        if (profesional == null) return NotFound();
        return Ok(profesional);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _profesionalService.EliminarAsync(id);
        if (!eliminado) return NotFound();
        return NoContent();
    }
}
