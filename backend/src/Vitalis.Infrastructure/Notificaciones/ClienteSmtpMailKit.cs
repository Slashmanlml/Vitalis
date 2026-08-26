using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Vitalis.Infrastructure.Notificaciones;

public class ClienteSmtpMailKit : IClienteSmtp
{
    private readonly NotificacionesOptions _options;

    public ClienteSmtpMailKit(IOptions<NotificacionesOptions> options)
    {
        _options = options.Value;
    }

    public async Task EnviarAsync(string remitenteNombre, string remitenteEmail, string destinatario, string asunto, string cuerpoHtml)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(remitenteNombre, remitenteEmail));
        message.To.Add(new MailboxAddress("", destinatario));
        message.Subject = asunto;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = cuerpoHtml
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        var secureSocketOptions = _options.Puerto == 465 
            ? SecureSocketOptions.SslOnConnect 
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(_options.Host, _options.Puerto, secureSocketOptions);

        if (!string.IsNullOrWhiteSpace(_options.Usuario) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            await client.AuthenticateAsync(_options.Usuario, _options.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
