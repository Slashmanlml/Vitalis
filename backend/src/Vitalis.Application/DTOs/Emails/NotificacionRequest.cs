namespace Vitalis.Application.DTOs.Emails;

public class NotificacionRequest
{
    public string Destinatario { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public int? TurnoId { get; set; }
    public Dictionary<string, string> Datos { get; set; } = new();
}
