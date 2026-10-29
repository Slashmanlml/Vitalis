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

    /// <summary>
    /// Dominios (separados por coma) cuyos correos se redirigen a
    /// <see cref="RedirigirTodoA"/>. Vacío = se redirige todo. Ejemplo para la
    /// demo: "email.com,vitalis.local" — los pacientes y usuarios de prueba
    /// nunca le escriben a nadie real, y quien se registra con su propio
    /// correo recibe el mail en su casilla.
    /// </summary>
    public string RedirigirSoloDominios { get; set; } = string.Empty;
    public int HorasAntesDelRecordatorio { get; set; } = 24;
    public int MinutosEntreBarridos { get; set; } = 30;
}
