namespace Vitalis.Application.DTOs.Bloqueos;

/// <summary>
/// Qué turnos quedarían cancelados si se creara un bloqueo con ese rango.
///
/// Crear un bloqueo cancela turnos y notifica pacientes, y es irreversible:
/// eliminar el bloqueo después libera el horario pero NO revive los turnos ni
/// desdice los correos ya enviados. Sin una previsualización, esa acción se
/// tomaba a ciegas. Este DTO alimenta la confirmación previa.
/// </summary>
public class ImpactoBloqueoDto
{
    public int CantidadTurnos { get; set; }
    public int PacientesAfectados { get; set; }
    /// <summary>Cuántos de esos pacientes recibirían aviso por correo. Es menor
    /// que PacientesAfectados cuando alguno no tiene email cargado.</summary>
    public int PacientesConEmail { get; set; }
    public List<TurnoAfectadoDto> Turnos { get; set; } = new();
}

public class TurnoAfectadoDto
{
    public int TurnoId { get; set; }
    public DateTime FechaHora { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool TieneEmail { get; set; }
}
