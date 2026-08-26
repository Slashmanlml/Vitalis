using Microsoft.EntityFrameworkCore;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly VitalisDbContext _context;

    public EmailService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var log = new EmailLog
        {
            Destinatario = to,
            Asunto = subject,
            Cuerpo = body,
            FechaEnvio = DateTime.UtcNow
        };

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();
        
        Console.WriteLine($"[EMAIL SIMULADO] Destinatario: {to} | Asunto: {subject}");
    }

    public async Task<IEnumerable<EmailLog>> GetEmailLogsAsync()
    {
        return await _context.EmailLogs
            .OrderByDescending(e => e.FechaEnvio)
            .ToListAsync();
    }

    public async Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion, string? asuntoPersonalizado = null, string? cuerpoPersonalizado = null)
    {
        string asunto;
        string cuerpo;

        switch (tipoNotificacion.ToLowerInvariant())
        {
            case "confirmacionturno":
                asunto = asuntoPersonalizado ?? "Confirmación de Turno Reservado - Vitalis";
                cuerpo = cuerpoPersonalizado ?? @"<div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b; background: #f8fafc; border-radius: 8px;'>
                    <div style='background: #0f766e; color: #fff; padding: 15px 20px; border-radius: 6px; text-align: center;'>
                        <h2 style='margin:0;'>¡Turno Confirmado con Éxito!</h2>
                    </div>
                    <div style='padding: 20px; background: #fff; margin-top: 15px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                        <p>Estimado/a paciente,</p>
                        <p>Le confirmamos que su turno médico ha sido <strong>aprobado y confirmado</strong> en la agenda del consultorio.</p>
                        <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                        <p><strong>Fecha estimada:</strong> Próxima cita programada</p>
                        <p><strong>Ubicación:</strong> Consultorio Central / Sala Virtual Vitalis</p>
                        <p><strong>Recomendación:</strong> Por favor presentarse 10 minutos antes con su DNI y credencial médica.</p>
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center; margin-top: 15px;'>Equipo Médico Vitalis - Sistema de Gestión de Consultorios</p>
                </div>";
                break;

            case "recordatorioturno":
                asunto = asuntoPersonalizado ?? "Recordatorio: Su cita médica es mañana - Vitalis";
                cuerpo = cuerpoPersonalizado ?? @"<div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b; background: #f8fafc; border-radius: 8px;'>
                    <div style='background: #d97706; color: #fff; padding: 15px 20px; border-radius: 6px; text-align: center;'>
                        <h2 style='margin:0;'>Recordatorio de Consulta Médica</h2>
                    </div>
                    <div style='padding: 20px; background: #fff; margin-top: 15px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                        <p>Estimado/a paciente,</p>
                        <p>Le recordamos que tiene una consulta médica programada para las próximas 24 horas.</p>
                        <p>Si no puede asistir, le solicitamos cancelar o reprogramar con anticipación para ceder el turno a otro paciente.</p>
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center; margin-top: 15px;'>Vitalis - Cuidando su salud</p>
                </div>";
                break;

            case "cancelacionturno":
                asunto = asuntoPersonalizado ?? "Aviso de Cancelación de Turno - Vitalis";
                cuerpo = cuerpoPersonalizado ?? @"<div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b; background: #f8fafc; border-radius: 8px;'>
                    <div style='background: #e11d48; color: #fff; padding: 15px 20px; border-radius: 6px; text-align: center;'>
                        <h2 style='margin:0;'>Cancelación de Turno Registrada</h2>
                    </div>
                    <div style='padding: 20px; background: #fff; margin-top: 15px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                        <p>Estimado/a paciente,</p>
                        <p>Le informamos que el turno previamente registrado ha sido <strong>cancelado</strong>.</p>
                        <p>Puede solicitar un nuevo turno en cualquier momento a través del portal de autogestión o en recepción.</p>
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center; margin-top: 15px;'>Vitalis - Atención al Paciente</p>
                </div>";
                break;

            case "nuevaprescripcion":
                asunto = asuntoPersonalizado ?? "Nueva Receta Médica Disponible - Vitalis";
                cuerpo = cuerpoPersonalizado ?? @"<div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b; background: #f8fafc; border-radius: 8px;'>
                    <div style='background: #0f766e; color: #fff; padding: 15px 20px; border-radius: 6px; text-align: center;'>
                        <h2 style='margin:0;'>Receta Médica Electrónica Emitida</h2>
                    </div>
                    <div style='padding: 20px; background: #fff; margin-top: 15px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                        <p>Estimado/a paciente,</p>
                        <p>Su médico tratante ha emitido una nueva orden médica/receta farmacológica.</p>
                        <p>Puede ingresar al sistema o acudir a la farmacia con el folio oficial emitido.</p>
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center; margin-top: 15px;'>Vitalis - Farmacología y Prescripciones</p>
                </div>";
                break;

            case "bienvenidapaciente":
                asunto = asuntoPersonalizado ?? "Bienvenido/a al Portal del Paciente - Vitalis";
                cuerpo = cuerpoPersonalizado ?? @"<div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b; background: #f8fafc; border-radius: 8px;'>
                    <div style='background: #0284c7; color: #fff; padding: 15px 20px; border-radius: 6px; text-align: center;'>
                        <h2 style='margin:0;'>¡Bienvenido a Vitalis!</h2>
                    </div>
                    <div style='padding: 20px; background: #fff; margin-top: 15px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                        <p>Estimado/a paciente,</p>
                        <p>Se ha creado con éxito su ficha clínica en nuestro sistema de consultorios médicos virtuales.</p>
                        <p>Ahora podrá gestionar sus turnos, consultar su historia médica y acceder a sus recetas de forma 100% digital.</p>
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center; margin-top: 15px;'>Equipo Vitalis - Plataforma de Gestión Médica</p>
                </div>";
                break;

            default:
                asunto = asuntoPersonalizado ?? "Notificación Informativa - Vitalis";
                cuerpo = cuerpoPersonalizado ?? @"<div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h3>Notificación de Consultorio Médico Vitalis</h3>
                    <p>Estimado/a paciente, le enviamos un mensaje informativo sobre sus citas médicas.</p>
                </div>";
                break;
        }

        var log = new EmailLog
        {
            Destinatario = to,
            Asunto = asunto,
            Cuerpo = cuerpo,
            FechaEnvio = DateTime.UtcNow
        };

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();

        Console.WriteLine($"[EMAIL SIMULADO] Tipo: {tipoNotificacion} | Destinatario: {to} | Asunto: {asunto}");
        return log;
    }

    public async Task<bool> EliminarLogAsync(int id)
    {
        var log = await _context.EmailLogs.FindAsync(id);
        if (log == null) return false;

        _context.EmailLogs.Remove(log);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LimpiarLogsAsync()
    {
        _context.EmailLogs.RemoveRange(_context.EmailLogs);
        await _context.SaveChangesAsync();
        return true;
    }
}
