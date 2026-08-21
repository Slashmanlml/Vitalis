using Vitalis.Application.DTOs.Turnos;

namespace Vitalis.Application.Interfaces;

public interface IReporteService
{
    Task<IEnumerable<TurnoDto>> TurnosPorProfesionalAsync(int profesionalId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<TurnoDto>> TurnosPorPacienteAsync(int pacienteId);
    Task<IEnumerable<TurnoDto>> TurnosPorObraSocialAsync(int obraSocialId);
    Task<object> EstadisticasGeneralesAsync();
}
