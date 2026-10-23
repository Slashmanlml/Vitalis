using Vitalis.Application.DTOs.Portal;

namespace Vitalis.Application.Interfaces;

/// <summary>
/// Datos del portal del paciente. Ningún método recibe un PacienteId: la ficha
/// se resuelve SIEMPRE a partir del usuario del token. Así un paciente no
/// puede pedir los turnos de otro cambiando un número en la URL (IDOR).
/// </summary>
public interface IPortalPacienteService
{
    Task<MiPerfilPacienteDto> ObtenerMiPerfilAsync(CancellationToken ct = default);
    Task<IEnumerable<MiTurnoDto>> ObtenerMisTurnosAsync(CancellationToken ct = default);
}
