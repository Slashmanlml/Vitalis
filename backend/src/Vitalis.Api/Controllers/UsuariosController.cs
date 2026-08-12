using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Usuarios;
using Vitalis.Application.Interfaces;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> ObtenerTodos([FromQuery] string? buscar)
    {
        var usuarios = await _usuarioService.ObtenerTodosAsync(buscar);
        return Ok(usuarios);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> ObtenerPorId(int id)
    {
        var usuario = await _usuarioService.ObtenerPorIdAsync(id);
        if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });
        return Ok(usuario);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioDto>> Crear([FromBody] CrearUsuarioDto dto)
    {
        var usuario = await _usuarioService.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> Editar(int id, [FromBody] EditarUsuarioDto dto)
    {
        var usuario = await _usuarioService.EditarAsync(id, dto);
        if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });
        return Ok(usuario);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var resultado = await _usuarioService.DesactivarAsync(id);
        if (!resultado) return NotFound(new { mensaje = "Usuario no encontrado." });
        return Ok(new { mensaje = "Usuario desactivado correctamente." });
    }
}
