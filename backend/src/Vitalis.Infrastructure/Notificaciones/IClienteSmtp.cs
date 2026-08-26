namespace Vitalis.Infrastructure.Notificaciones;

public interface IClienteSmtp
{
    Task EnviarAsync(string remitenteNombre, string remitenteEmail, string destinatario, string asunto, string cuerpoHtml);
}
