using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Medicamentos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicamentosController : ControllerBase
{
    private readonly IMedicamentoService _service;
    public MedicamentosController(IMedicamentoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? buscar)
    {
        return Ok(await _service.ObtenerTodosAsync(buscar));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var med = await _service.ObtenerPorIdAsync(id);
        if (med == null) return NotFound();
        return Ok(med);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearMedicamentoDto dto)
    {
        var med = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = med.Id }, med);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] EditarMedicamentoDto dto)
    {
        var med = await _service.EditarAsync(id, dto);
        if (med == null) return NotFound();
        return Ok(med);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.EliminarAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}
