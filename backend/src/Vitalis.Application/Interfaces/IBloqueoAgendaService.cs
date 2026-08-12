using Vitalis.Application.DTOs.Bloqueos;

namespace Vitalis.Application.Interfaces;

public interface IBloqueoAgendaService
{
    Task<IEnumerable<BloqueoAgendaDto>> ObtenerTodosAsync();
    Task<IEnumerable<BloqueoAgendaDto>> ObtenerPorProfesionalAsync(int profesionalId);
    Task<BloqueoAgendaDto?> ObtenerPorIdAsync(int id);
    Task<BloqueoAgendaDto> CrearAsync(CrearBloqueoDto dto);
    Task<bool> EliminarAsync(int id);
    Task<bool> EsHorarioBloqueadoAsync(int profesionalId, DateTime fechaHora);
}
