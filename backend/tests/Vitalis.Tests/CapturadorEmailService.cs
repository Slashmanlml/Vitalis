using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;

namespace Vitalis.Tests;

/// <summary>
/// Doble de IEmailService que guarda los pedidos completos (con sus Datos),
/// para poder leer el enlace de confirmación como lo haría la persona.
/// </summary>
public class CapturadorEmailService : IEmailService
{
    public List<NotificacionRequest> Pedidos { get; } = new();

    public NotificacionRequest? Ultimo(string evento) => Pedidos.LastOrDefault(p => p.Evento == evento);

    /// <summary>Extrae el token del último enlace de confirmación enviado.</summary>
    public string? UltimoToken(string evento)
    {
        var url = Ultimo(evento)?.Datos.GetValueOrDefault("UrlConfirmacion");
        if (url is null) return null;
        var i = url.IndexOf("token=", System.StringComparison.Ordinal);
        return i < 0 ? null : System.Uri.UnescapeDataString(url[(i + 6)..]);
    }

    public Task<bool> NotificarAsync(NotificacionRequest request)
    {
        Pedidos.Add(request);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<EmailLog>> GetEmailLogsAsync(string? origen = null, string? evento = null, string? estado = null) =>
        Task.FromResult<IEnumerable<EmailLog>>(new List<EmailLog>());

    public Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion, string? asuntoPersonalizado = null, string? cuerpoPersonalizado = null) =>
        Task.FromResult(new EmailLog());

    public Task<bool> EliminarLogAsync(int id) => Task.FromResult(true);
}
