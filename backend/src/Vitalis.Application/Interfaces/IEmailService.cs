using Vitalis.Domain.Entities;

namespace Vitalis.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task<IEnumerable<EmailLog>> GetEmailLogsAsync();
    Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion, string? asuntoPersonalizado = null, string? cuerpoPersonalizado = null);
    Task<bool> EliminarLogAsync(int id);
    Task<bool> LimpiarLogsAsync();
}
