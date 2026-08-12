using Vitalis.Application.DTOs.Especialidades;

namespace Vitalis.Application.Interfaces;

public interface IEspecialidadService
{
    Task<List<EspecialidadDto>> ObtenerTodosAsync(string? buscar = null);
    Task<EspecialidadDto?> ObtenerPorIdAsync(int id);
    Task<EspecialidadDto> CrearAsync(CrearEspecialidadDto dto);
    Task<EspecialidadDto?> EditarAsync(int id, EditarEspecialidadDto dto);
    Task<bool> EliminarAsync(int id);
}
