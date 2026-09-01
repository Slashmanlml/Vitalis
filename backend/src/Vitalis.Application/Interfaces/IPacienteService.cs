using Vitalis.Application.DTOs.Pacientes;

namespace Vitalis.Application.Interfaces;

public interface IPacienteService
{
    /// <summary>
    /// Listado de pacientes. Por omisión devuelve solo los activos.
    /// </summary>
    /// <param name="incluirInactivos">
    /// Cuando es true incluye también los dados de baja. La baja de un paciente es
    /// lógica —su historia clínica debe conservarse— pero hasta esta versión el
    /// listado no ofrecía forma de verlos ni de reactivarlos: un paciente dado de
    /// baja por error quedaba invisible de manera permanente.
    /// </param>
    Task<List<PacienteDto>> ObtenerTodosAsync(string? buscar = null, bool incluirInactivos = false);
    Task<PacienteDto?> ObtenerPorIdAsync(int id);
    Task<PacienteDto> CrearAsync(CrearPacienteDto dto);
    Task<PacienteDto?> EditarAsync(int id, EditarPacienteDto dto);
    Task<bool> DesactivarAsync(int id);
    Task<bool> ReactivarAsync(int id);
}