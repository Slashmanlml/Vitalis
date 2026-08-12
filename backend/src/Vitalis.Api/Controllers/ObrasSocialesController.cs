using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.ObrasSociales;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ObrasSocialesController : ControllerBase
{
    private readonly IObraSocialService _obraSocialService;

    public ObrasSocialesController(IObraSocialService obraSocialService)
    {
        _obraSocialService = obraSocialService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var obras = await _obraSocialService.ObtenerTodasAsync();
        return Ok(obras);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var obra = await _obraSocialService.ObtenerPorIdAsync(id);
        if (obra == null) return NotFound();
        return Ok(obra);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CrearObraSocialDto dto)
    {
        var obra = await _obraSocialService.CrearAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = obra.Id }, obra);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] EditarObraSocialDto dto)
    {
        var obra = await _obraSocialService.EditarAsync(id, dto);
        if (obra == null) return NotFound();
        return Ok(obra);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _obraSocialService.EliminarAsync(id);
        if (!eliminado) return NotFound();
        return NoContent();
    }
}
