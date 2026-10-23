namespace Vitalis.Application.DTOs.Emails;

public class NotificacionRequest
{
    public string Destinatario { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public int? TurnoId { get; set; }
    public Dictionary<string, string> Datos { get; set; } = new();

    /// <summary>
    /// Claves de Datos cuyo valor NO debe quedar guardado en el historial de
    /// correos (EmailLog) cuando el mail se envía de verdad. Caso típico: el
    /// enlace de confirmación lleva un token que da acceso a la cuenta; si
    /// quedara en el historial, cualquier administrador podría usarlo.
    /// </summary>
    public List<string> DatosSensibles { get; set; } = new();
}
