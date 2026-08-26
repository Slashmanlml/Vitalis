using Vitalis.Application.DTOs.Bloqueos;

namespace Vitalis.Application.Interfaces;

public interface IBloqueoAgendaService
{
    Task<IEnumerable<BloqueoAgendaDto>> ObtenerTodosAsync();
    Task<IEnumerable<BloqueoAgendaDto>> ObtenerPorProfesionalAsync(int profesionalId);
    Task<BloqueoAgendaDto?> ObtenerPorIdAsync(int id);
    Task<BloqueoAgendaDto> CrearAsync(CrearBloqueoDto dto);

    /// <summary>
    /// Simula el bloqueo sin aplicarlo: devuelve los turnos que quedarían
    /// cancelados. Permite confirmar con el número a la vista en vez de a ciegas.
    /// </summary>
    Task<ImpactoBloqueoDto> ObtenerImpactoAsync(int profesionalId, DateTime desde, DateTime hasta);
    Task<bool> EliminarAsync(int id);
    Task<bool> EsHorarioBloqueadoAsync(int profesionalId, DateTime fechaHora);
}
