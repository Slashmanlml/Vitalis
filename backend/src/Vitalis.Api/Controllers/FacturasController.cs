using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Facturas;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Facturacion)]
public class FacturasController : ControllerBase
{
    private readonly IFacturaService _service;
    public FacturasController(IFacturaService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.ObtenerTodasAsync());
    }

    [HttpGet("paciente/{pacienteId}")]
    public async Task<IActionResult> GetByPaciente(int pacienteId)
    {
        return Ok(await _service.ObtenerPorPacienteAsync(pacienteId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var f = await _service.ObtenerPorIdAsync(id);
        if (f == null) return NotFound();
        return Ok(f);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearFacturaDto dto)
    {
        var f = await _service.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = f.Id }, f);
    }

    [HttpPost("pago")]
    public async Task<IActionResult> RegistrarPago([FromBody] RegistrarPagoDto dto)
    {
        var f = await _service.RegistrarPagoAsync(dto);
        return Ok(f);
    }
}
