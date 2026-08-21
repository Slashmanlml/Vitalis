using Vitalis.Application.DTOs.Turnos;

namespace Vitalis.Application.Interfaces;

public interface ITurnoService
{
    Task<IEnumerable<TurnoDto>> ObtenerTodosAsync();
    Task<TurnoDto?> ObtenerPorIdAsync(int id);
    Task<TurnoDto> CrearAsync(CrearTurnoDto dto);
    Task<TurnoDto?> EditarAsync(int id, EditarTurnoDto dto);
    Task<bool> EliminarAsync(int id);
}

