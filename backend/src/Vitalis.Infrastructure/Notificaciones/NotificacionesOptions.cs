namespace Vitalis.Infrastructure.Notificaciones;

public class NotificacionesOptions
{
    public const string SectionName = "Notificaciones";

    public bool Habilitado { get; set; } = true;
    public string Host { get; set; } = "smtp-relay.brevo.com";
    public int Puerto { get; set; } = 587;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = "Vitalis";
    public string RemitenteEmail { get; set; } = "no-responder@vitalis.local";
    public bool ModoPrueba { get; set; } = false;
    public string RedirigirTodoA { get; set; } = string.Empty;
    public int HorasAntesDelRecordatorio { get; set; } = 24;
    public int MinutosEntreBarridos { get; set; } = 30;
}
