using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Prestaciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrestacionesController : ControllerBase
{
    private readonly IPrestacionService _service;
    public PrestacionesController(IPrestacionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.ObtenerTodasAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _service.ObtenerPorIdAsync(id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [Authorize(Roles = Roles.Administrador + "," + Roles.Facturacion)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearPrestacionDto dto)
    {
        var p = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = p.Id }, p);
    }

    [Authorize(Roles = Roles.Administrador + "," + Roles.Facturacion)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] EditarPrestacionDto dto)
    {
        var p = await _service.EditarAsync(id, dto);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [Authorize(Roles = Roles.Administrador + "," + Roles.Facturacion)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.EliminarAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}
