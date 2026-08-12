using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Liquidaciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Facturacion)]
public class LiquidacionesController : ControllerBase
{
    private readonly ILiquidacionService _service;
    public LiquidacionesController(ILiquidacionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.ObtenerTodasAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var f = await _service.ObtenerPorIdAsync(id);
        if (f == null) return NotFound();
        return Ok(f);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearLiquidacionDto dto)
    {
        var f = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = f.Id }, f);
    }

    [HttpPost("{id}/liquidar")]
    public async Task<IActionResult> Liquidar(int id)
    {
        var f = await _service.LiquidarAsync(id);
        if (f == null) return NotFound();
        return Ok(f);
    }
}
