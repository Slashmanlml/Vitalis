using Vitalis.Application.DTOs.Pacientes;

namespace Vitalis.Application.Interfaces;

public interface IPacienteService
{
    Task<List<PacienteDto>> ObtenerTodosAsync(string? buscar = null);
    Task<PacienteDto?> ObtenerPorIdAsync(int id);
    Task<PacienteDto> CrearAsync(CrearPacienteDto dto);
    Task<PacienteDto?> EditarAsync(int id, EditarPacienteDto dto);
    Task<bool> DesactivarAsync(int id);
}