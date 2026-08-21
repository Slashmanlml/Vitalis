using Vitalis.Application.DTOs.Usuarios;

namespace Vitalis.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioDto>> ObtenerTodosAsync(string? buscar);
    Task<UsuarioDto?> ObtenerPorIdAsync(int id);
    Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto);
    Task<UsuarioDto?> EditarAsync(int id, EditarUsuarioDto dto);
    Task<bool> DesactivarAsync(int id);
}
