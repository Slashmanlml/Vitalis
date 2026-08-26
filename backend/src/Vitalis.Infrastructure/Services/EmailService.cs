using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Notificaciones;

namespace Vitalis.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly VitalisDbContext _context;
    private readonly IClienteSmtp _smtpClient;
    private readonly NotificacionesOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        VitalisDbContext context,
        IClienteSmtp smtpClient,
        IOptions<NotificacionesOptions> options,
        ILogger<EmailService> logger)
    {
        _context = context;
        _smtpClient = smtpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> NotificarAsync(NotificacionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Destinatario))
        {
            _logger.LogWarning("Se intentó enviar una notificación sin destinatario válido. Evento: {Evento}", request.Evento);
            return false;
        }

        var (asunto, cuerpo) = PlantillasEmail.Generar(request.Evento, request.Datos);

        var log = new EmailLog
        {
            Destinatario = request.Destinatario,
            Asunto = asunto,
            Cuerpo = cuerpo,
            FechaEnvio = DateTime.UtcNow,
            Origen = OrigenNotificacion.Sistema,
            Evento = string.IsNullOrWhiteSpace(request.Evento) ? EventoNotificacion.Personalizado : request.Evento,
            TurnoId = request.TurnoId
        };

        if (!_options.Habilitado)
        {
            log.Estado = EstadoNotificacion.Simulado;
            log.MensajeError = "El módulo de notificaciones se encuentra deshabilitado por configuración.";
            _context.EmailLogs.Add(log);
            await _context.SaveChangesAsync();
            return true;
        }

        if (_options.ModoPrueba)
        {
            log.Estado = EstadoNotificacion.Simulado;
            _context.EmailLogs.Add(log);
            await _context.SaveChangesAsync();
            _logger.LogInformation("[MODO PRUEBA] Correo simulado para {Destinatario}. Evento: {Evento}", request.Destinatario, request.Evento);
            return true;
        }

        // Envío Real SMTP
        try
        {
            string destinatarioReal = !string.IsNullOrWhiteSpace(_options.RedirigirTodoA) 
                ? _options.RedirigirTodoA 
                : request.Destinatario;

            string asuntoEnvio = !string.IsNullOrWhiteSpace(_options.RedirigirTodoA)
                ? $"[Para: {request.Destinatario}] {asunto}"
                : asunto;

            await _smtpClient.EnviarAsync(
                _options.RemitenteNombre,
                _options.RemitenteEmail,
                destinatarioReal,
                asuntoEnvio,
                cuerpo
            );

            log.Estado = EstadoNotificacion.Enviado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al enviar notificación por SMTP a {Destinatario}. Evento: {Evento}", request.Destinatario, request.Evento);
            log.Estado = EstadoNotificacion.Fallido;
            log.MensajeError = ex.Message.Length > 1000 ? ex.Message.Substring(0, 1000) : ex.Message;
        }

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();

        return log.Estado == EstadoNotificacion.Enviado;
    }

    public async Task<IEnumerable<EmailLog>> GetEmailLogsAsync(string? origen = null, string? evento = null, string? estado = null)
    {
        var query = _context.EmailLogs
            .Include(e => e.Turno)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(origen))
        {
            query = query.Where(e => e.Origen == origen);
        }

        if (!string.IsNullOrWhiteSpace(evento))
        {
            query = query.Where(e => e.Evento == evento);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(e => e.Estado == estado);
        }

        return await query
            .OrderByDescending(e => e.FechaEnvio)
            .ToListAsync();
    }

    public async Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion, string? asuntoPersonalizado = null, string? cuerpoPersonalizado = null)
    {
        var datos = new Dictionary<string, string>
        {
            ["PacienteNombre"] = "Paciente de Prueba",
            ["ProfesionalNombre"] = "Dr. Alejandro Gómez",
            ["FechaHora"] = DateTime.Now.AddDays(1).ToString("dd/MM/yyyy HH:mm"),
            ["Especialidad"] = "Medicina General",
            ["HorasRestantes"] = "24",
            ["DetalleMedicamentos"] = "<ul><li><strong>Amoxicilina 500mg</strong>: 1 comprimido cada 8hs por 7 días</li></ul>",
            ["Indicaciones"] = "Reposo relativo y abundante hidratación.",
            ["Asunto"] = asuntoPersonalizado ?? "Notificación de Prueba Simulada",
            ["Cuerpo"] = cuerpoPersonalizado ?? "Este es un correo emitido manualmente con fines de demostración y prueba de plantillas."
        };

        var (asunto, cuerpo) = PlantillasEmail.Generar(tipoNotificacion, datos);

        if (!string.IsNullOrWhiteSpace(asuntoPersonalizado)) asunto = asuntoPersonalizado;
        if (!string.IsNullOrWhiteSpace(cuerpoPersonalizado)) cuerpo = cuerpoPersonalizado;

        var log = new EmailLog
        {
            Destinatario = to,
            Asunto = asunto,
            Cuerpo = cuerpo,
            FechaEnvio = DateTime.UtcNow,
            Origen = OrigenNotificacion.Simulado,
            Evento = string.IsNullOrWhiteSpace(tipoNotificacion) ? EventoNotificacion.Personalizado : tipoNotificacion,
            Estado = EstadoNotificacion.Simulado
        };

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation("[SIMULACIÓN MANUAL] Registrada notificación para {Destinatario}. Evento: {Evento}", to, tipoNotificacion);
        return log;
    }

    public async Task<bool> EliminarLogAsync(int id)
    {
        var log = await _context.EmailLogs.FindAsync(id);
        if (log == null) return false;

        if (log.Origen == OrigenNotificacion.Sistema)
        {
            throw new ConflictException("No se pueden eliminar notificaciones emitidas por el sistema.");
        }

        _context.EmailLogs.Remove(log);
        await _context.SaveChangesAsync();
        return true;
    }
}
