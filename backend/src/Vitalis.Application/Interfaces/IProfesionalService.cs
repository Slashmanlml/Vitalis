using Vitalis.Application.DTOs.Profesionales;

namespace Vitalis.Application.Interfaces;

public interface IProfesionalService
{
    Task<IEnumerable<ProfesionalDto>> ObtenerTodosAsync();
    Task<ProfesionalDto?> ObtenerPorIdAsync(int id);
    Task<ProfesionalDto> CrearAsync(CrearProfesionalDto dto);
    Task<ProfesionalDto?> EditarAsync(int id, EditarProfesionalDto dto);
    Task<bool> EliminarAsync(int id);
}
