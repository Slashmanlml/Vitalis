using Vitalis.Domain.Constants;

namespace Vitalis.Domain.Entities;

public class EmailLog
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

    /// <summary>"Sistema" si lo emitió un evento de negocio; "Simulado" si lo
    /// generó un administrador desde la pantalla.</summary>
    public string Origen { get; set; } = OrigenNotificacion.Sistema;

    /// <summary>Qué lo disparó. Ver la clase EventoNotificacion.</summary>
    public string Evento { get; set; } = EventoNotificacion.Personalizado;

    /// <summary>Turno que originó la notificación, cuando aplica.</summary>
    public int? TurnoId { get; set; }
    public Turno? Turno { get; set; }

    /// <summary>"Enviado" | "Fallido" | "Simulado".</summary>
    public string Estado { get; set; } = EstadoNotificacion.Enviado;

    /// <summary>Detalle del error cuando Estado = "Fallido".</summary>
    public string? MensajeError { get; set; }
}
