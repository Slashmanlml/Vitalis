using Vitalis.Application.DTOs.Emails;
using Vitalis.Domain.Entities;

namespace Vitalis.Application.Interfaces;

public interface IEmailService
{
    /// <summary>Envía y registra. Nunca lanza: ante una falla registra
    /// Estado="Fallido" y devuelve false.</summary>
    Task<bool> NotificarAsync(NotificacionRequest request);

    Task<IEnumerable<EmailLog>> GetEmailLogsAsync(string? origen = null, string? evento = null, string? estado = null);

    /// <summary>Alta manual desde la pantalla. Fuerza Origen="Simulado".</summary>
    Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion,
                                     string? asuntoPersonalizado = null,
                                     string? cuerpoPersonalizado = null);

    Task<bool> EliminarLogAsync(int id);
}
