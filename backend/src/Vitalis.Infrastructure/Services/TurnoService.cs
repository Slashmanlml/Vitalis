using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Turnos;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;

namespace Vitalis.Infrastructure.Services;

public class TurnoService : ITurnoService
{
    private readonly VitalisDbContext _context;
    private readonly IEmailService _emailService;

    public TurnoService(VitalisDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<IEnumerable<TurnoDto>> ObtenerTodosAsync()
    {
        return await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .Include(t => t.ObraSocial)
            .Select(t => new TurnoDto
            {
                Id = t.Id,
                PacienteId = t.PacienteId,
                PacienteNombre = t.Paciente.Nombre + " " + t.Paciente.Apellido,
                ProfesionalId = t.ProfesionalId,
                ProfesionalNombre = t.Profesional.Nombre + " " + t.Profesional.Apellido,
                ObraSocialId = t.ObraSocialId,
                ObraSocialNombre = t.ObraSocial.Nombre,
                FechaHora = t.FechaHora,
                Confirmado = t.Confirmado,
                Estado = t.Estado
            })
            .OrderByDescending(t => t.FechaHora)
            .ToListAsync();
    }

    public async Task<TurnoDto?> ObtenerPorIdAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .Include(t => t.ObraSocial)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (turno == null) return null;

        return new TurnoDto
        {
            Id = turno.Id,
            PacienteId = turno.PacienteId,
            PacienteNombre = turno.Paciente.Nombre + " " + turno.Paciente.Apellido,
            ProfesionalId = turno.ProfesionalId,
            ProfesionalNombre = turno.Profesional.Nombre + " " + turno.Profesional.Apellido,
            ObraSocialId = turno.ObraSocialId,
            ObraSocialNombre = turno.ObraSocial.Nombre,
            FechaHora = turno.FechaHora,
            Confirmado = turno.Confirmado,
            Estado = turno.Estado
        };
    }

    private async Task ValidarLógicaComplejaTurnoAsync(int pacienteId, int profesionalId, DateTime fechaHora, int? idExcluido = null)
    {
        if (fechaHora < DateTime.UtcNow)
        {
            throw new ValidationException("No se pueden agendar turnos en el pasado.");
        }

        // 1. Días laborales (Lunes a Viernes)
        var dayOfWeek = fechaHora.ToLocalTime().DayOfWeek;
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
        {
            throw new ValidationException("No se pueden agendar turnos los fines de semana.");
        }

        // 2. Horario laboral (8:00 AM a 8:00 PM)
        var time = fechaHora.ToLocalTime().TimeOfDay;
        if (time < new TimeSpan(8, 0, 0) || time > new TimeSpan(20, 0, 0))
        {
            throw new ValidationException("Los turnos deben agendarse dentro del horario de atención (8:00 AM a 8:00 PM).");
        }

        // Rangos de superposición de 30 minutos
        var inicioRango = fechaHora.AddMinutes(-29);
        var finRango = fechaHora.AddMinutes(29);

        // 3. Superposición del Médico
        var existeSuperposicionMedico = await _context.Turnos
            .AnyAsync(t => t.ProfesionalId == profesionalId 
                           && t.FechaHora >= inicioRango 
                           && t.FechaHora <= finRango 
                           && t.Estado != "Cancelado"
                           && t.Id != idExcluido);
        if (existeSuperposicionMedico)
        {
            throw new ConflictException("El médico ya tiene asignado un turno en ese rango horario (se requiere un intervalo de 30 minutos).");
        }

        // 4. Superposición del Paciente
        var existeSuperposicionPaciente = await _context.Turnos
            .AnyAsync(t => t.PacienteId == pacienteId 
                           && t.FechaHora >= inicioRango 
                           && t.FechaHora <= finRango 
                           && t.Estado != "Cancelado"
                           && t.Id != idExcluido);
        if (existeSuperposicionPaciente)
        {
            throw new ConflictException("El paciente ya tiene otro turno programado en ese rango horario.");
        }

        // 5. Coincidencia con Agenda Bloqueada
        var turnoFin = fechaHora.AddMinutes(30);
        var estaBloqueado = await _context.BloqueosAgenda
            .AnyAsync(b => b.ProfesionalId == profesionalId
                           && b.FechaHoraInicio < turnoFin
                           && b.FechaHoraFin > fechaHora);
        if (estaBloqueado)
        {
            throw new ConflictException("El horario seleccionado está bloqueado por el médico.");
        }
    }

    public async Task<TurnoDto> CrearAsync(CrearTurnoDto dto)
    {
        // Se normaliza a Utc antes de usarla en cualquier lado: tanto la validación
        // (que consulta la base) como la entidad necesitan Kind=Utc, porque Npgsql
        // rechaza DateTimeKind.Unspecified tanto en parámetros de consulta como al guardar.
        var fechaHoraUtc = DateTime.SpecifyKind(dto.FechaHora, DateTimeKind.Utc);

        await ValidarLógicaComplejaTurnoAsync(dto.PacienteId, dto.ProfesionalId, fechaHoraUtc);

        var turno = new Turno
        {
            PacienteId = dto.PacienteId,
            ProfesionalId = dto.ProfesionalId,
            ObraSocialId = dto.ObraSocialId,
            FechaHora = fechaHoraUtc,
            Confirmado = false,
            Estado = "Solicitado"
        };

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        // Enviar Correo de Confirmación de Turno
        var pac = await _context.Pacientes.FindAsync(turno.PacienteId);
        var prof = await _context.Profesionales.FindAsync(turno.ProfesionalId);
        if (pac != null && prof != null)
        {
            string fechaStr = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            string asunto = "Confirmación de Turno Reservado - Vitalis";
            string cuerpo = $@"<div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                <h2 style='color: #2b7a78;'>¡Su turno ha sido reservado!</h2>
                <p>Estimado/a <strong>{pac.Nombre} {pac.Apellido}</strong>,</p>
                <p>Le confirmamos que se ha agendado exitosamente su turno en nuestro consultorio.</p>
                <hr style='border: 0; border-top: 1px solid #ccc;'/>
                <p><strong>Médico:</strong> Dr/Dra. {prof.Nombre} {prof.Apellido}</p>
                <p><strong>Fecha y Hora:</strong> {fechaStr}</p>
                <p><strong>Estado del Turno:</strong> Solicitado</p>
                <hr style='border: 0; border-top: 1px solid #ccc;'/>
                <p>Si necesita cancelar o reprogramar su cita, por favor hágalo con anticipación.</p>
                <br/>
                <p>Atentamente,<br/><strong>Equipo Vitalis</strong></p>
            </div>";
            await _emailService.SendEmailAsync(pac.Email ?? "paciente@vitalis.local", asunto, cuerpo);
        }

        return await ObtenerPorIdAsync(turno.Id) ?? throw new Exception("Error al crear turno");
    }

    public async Task<TurnoDto?> EditarAsync(int id, EditarTurnoDto dto)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return null;

        var fechaHoraUtc = DateTime.SpecifyKind(dto.FechaHora, DateTimeKind.Utc);

        // Validar lógica si cambió fecha, médico o paciente
        if (turno.FechaHora != fechaHoraUtc || turno.ProfesionalId != dto.ProfesionalId || turno.PacienteId != dto.PacienteId)
        {
            await ValidarLógicaComplejaTurnoAsync(dto.PacienteId, dto.ProfesionalId, fechaHoraUtc, id);
        }

        var fechaAnterior = turno.FechaHora;
        var estadoAnterior = turno.Estado;
        var confirmadoAnterior = turno.Confirmado;

        turno.PacienteId = dto.PacienteId;
        turno.ProfesionalId = dto.ProfesionalId;
        turno.ObraSocialId = dto.ObraSocialId;
        turno.FechaHora = fechaHoraUtc;
        turno.Confirmado = dto.Confirmado;
        if (!string.IsNullOrWhiteSpace(dto.Estado))
            turno.Estado = dto.Estado;

        await _context.SaveChangesAsync();

        var pac = await _context.Pacientes.FindAsync(turno.PacienteId);
        var prof = await _context.Profesionales.FindAsync(turno.ProfesionalId);

        // Notificar Reprogramación si cambió fecha y hora
        if (turno.FechaHora != fechaAnterior && pac != null && prof != null)
        {
            string fechaAntStr = fechaAnterior.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            string fechaNuevaStr = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            string asunto = "Reprogramación de Turno - Vitalis";
            string cuerpo = $@"<div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                <h2 style='color: #e0a96d;'>Su turno ha sido reprogramado</h2>
                <p>Estimado/a <strong>{pac.Nombre} {pac.Apellido}</strong>,</p>
                <p>Le informamos que su turno con el profesional <strong>Dr/Dra. {prof.Nombre} {prof.Apellido}</strong> ha sido reprogramado.</p>
                <hr style='border: 0; border-top: 1px solid #ccc;'/>
                <p><strong>Horario Anterior:</strong> {fechaAntStr}</p>
                <p><strong>Nuevo Horario:</strong> {fechaNuevaStr}</p>
                <hr style='border: 0; border-top: 1px solid #ccc;'/>
                <p>Atentamente,<br/><strong>Equipo Vitalis</strong></p>
            </div>";
            await _emailService.SendEmailAsync(pac.Email ?? "paciente@vitalis.local", asunto, cuerpo);
        }

        // Notificar Cancelación
        if (turno.Estado == "Cancelado" && estadoAnterior != "Cancelado" && pac != null && prof != null)
        {
            string fechaStr = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            string asunto = "Cancelación de Turno Confirmada - Vitalis";
            string cuerpo = $@"<div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                <h2 style='color: #d9534f;'>Cancelación de Turno</h2>
                <p>Estimado/a <strong>{pac.Nombre} {pac.Apellido}</strong>,</p>
                <p>Le informamos que su turno para el día <strong>{fechaStr}</strong> con el profesional <strong>Dr/Dra. {prof.Nombre} {prof.Apellido}</strong> ha sido cancelado.</p>
                <br/>
                <p>Atentamente,<br/><strong>Equipo Vitalis</strong></p>
            </div>";
            await _emailService.SendEmailAsync(pac.Email ?? "paciente@vitalis.local", asunto, cuerpo);
        }

        // Notificar Confirmación
        if (turno.Confirmado && !confirmadoAnterior && pac != null && prof != null)
        {
            string fechaStr = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            string asunto = "Turno Confirmado Oficialmente - Vitalis";
            string cuerpo = $@"<div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b; background: #f8fafc; border-radius: 8px;'>
                <div style='background: #0f766e; color: #fff; padding: 15px 20px; border-radius: 6px; text-align: center;'>
                    <h2 style='margin:0;'>¡Su turno ha sido confirmado!</h2>
                </div>
                <div style='padding: 20px; background: #fff; margin-top: 15px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                    <p>Estimado/a <strong>{pac.Nombre} {pac.Apellido}</strong>,</p>
                    <p>Le informamos que su turno para el día <strong>{fechaStr}</strong> con el profesional <strong>Dr/Dra. {prof.Nombre} {prof.Apellido}</strong> ha sido confirmado en la agenda.</p>
                </div>
                <p style='font-size: 12px; color: #64748b; text-align: center; margin-top: 15px;'>Equipo Vitalis - Consultorios Médicos</p>
            </div>";
            await _emailService.SendEmailAsync(pac.Email ?? "paciente@vitalis.local", asunto, cuerpo);
        }

        return await ObtenerPorIdAsync(turno.Id);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return false;

        // Enviar mail antes de eliminar (o si se marca como cancelado)
        var pac = await _context.Pacientes.FindAsync(turno.PacienteId);
        var prof = await _context.Profesionales.FindAsync(turno.ProfesionalId);
        if (pac != null && prof != null)
        {
            string fechaStr = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            string asunto = "Cancelación de Turno - Vitalis";
            string cuerpo = $@"<div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                <h2 style='color: #d9534f;'>Cancelación de Turno</h2>
                <p>Estimado/a <strong>{pac.Nombre} {pac.Apellido}</strong>,</p>
                <p>Le informamos que su turno para el día <strong>{fechaStr}</strong> con el profesional <strong>Dr/Dra. {prof.Nombre} {prof.Apellido}</strong> ha sido cancelado.</p>
                <br/>
                <p>Atentamente,<br/><strong>Equipo Vitalis</strong></p>
            </div>";
            await _emailService.SendEmailAsync(pac.Email ?? "paciente@vitalis.local", asunto, cuerpo);
        }

        _context.Turnos.Remove(turno);
        await _context.SaveChangesAsync();
        return true;
    }
}
