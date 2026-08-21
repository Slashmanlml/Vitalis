using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Bloqueos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class BloqueoAgendaService : IBloqueoAgendaService
{
    private readonly VitalisDbContext _context;
    private readonly IEmailService _emailService;

    public BloqueoAgendaService(VitalisDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<IEnumerable<BloqueoAgendaDto>> ObtenerTodosAsync()
    {
        return await _context.BloqueosAgenda
            .Include(b => b.Profesional)
            .Select(b => new BloqueoAgendaDto
            {
                Id = b.Id,
                ProfesionalId = b.ProfesionalId,
                ProfesionalNombre = b.Profesional.Nombre + " " + b.Profesional.Apellido,
                FechaHoraInicio = b.FechaHoraInicio,
                FechaHoraFin = b.FechaHoraFin,
                Motivo = b.Motivo
            })
            .OrderByDescending(b => b.FechaHoraInicio)
            .ToListAsync();
    }

    public async Task<IEnumerable<BloqueoAgendaDto>> ObtenerPorProfesionalAsync(int profesionalId)
    {
        return await _context.BloqueosAgenda
            .Include(b => b.Profesional)
            .Where(b => b.ProfesionalId == profesionalId)
            .Select(b => new BloqueoAgendaDto
            {
                Id = b.Id,
                ProfesionalId = b.ProfesionalId,
                ProfesionalNombre = b.Profesional.Nombre + " " + b.Profesional.Apellido,
                FechaHoraInicio = b.FechaHoraInicio,
                FechaHoraFin = b.FechaHoraFin,
                Motivo = b.Motivo
            })
            .OrderByDescending(b => b.FechaHoraInicio)
            .ToListAsync();
    }

    public async Task<BloqueoAgendaDto?> ObtenerPorIdAsync(int id)
    {
        var b = await _context.BloqueosAgenda
            .Include(b => b.Profesional)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (b == null) return null;

        return new BloqueoAgendaDto
        {
            Id = b.Id,
            ProfesionalId = b.ProfesionalId,
            ProfesionalNombre = b.Profesional.Nombre + " " + b.Profesional.Apellido,
            FechaHoraInicio = b.FechaHoraInicio,
            FechaHoraFin = b.FechaHoraFin,
            Motivo = b.Motivo
        };
    }

    public async Task<BloqueoAgendaDto> CrearAsync(CrearBloqueoDto dto)
    {
        // Se normaliza a Utc antes de usarla en cualquier consulta o guardado: Npgsql
        // rechaza DateTimeKind.Unspecified tanto en parámetros de consulta como al guardar.
        var fechaHoraInicioUtc = DateTime.SpecifyKind(dto.FechaHoraInicio, DateTimeKind.Utc);
        var fechaHoraFinUtc = DateTime.SpecifyKind(dto.FechaHoraFin, DateTimeKind.Utc);

        if (fechaHoraInicioUtc >= fechaHoraFinUtc)
        {
            throw new ValidationException("La fecha de inicio debe ser anterior a la de fin.");
        }

        if (fechaHoraInicioUtc < DateTime.UtcNow)
        {
            throw new ValidationException("No se pueden crear bloqueos en el pasado.");
        }

        var profesional = await _context.Profesionales.FindAsync(dto.ProfesionalId);
        if (profesional == null)
        {
            throw new NotFoundException("Profesional no encontrado.");
        }

        // Crear el bloqueo
        var bloqueo = new BloqueoAgenda
        {
            ProfesionalId = dto.ProfesionalId,
            FechaHoraInicio = fechaHoraInicioUtc,
            FechaHoraFin = fechaHoraFinUtc,
            Motivo = dto.Motivo
        };

        _context.BloqueosAgenda.Add(bloqueo);
        await _context.SaveChangesAsync();

        // Buscar y cancelar turnos superpuestos
        var turnosSuperpuestos = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .Where(t => t.ProfesionalId == dto.ProfesionalId
                        && t.FechaHora >= fechaHoraInicioUtc
                        && t.FechaHora <= fechaHoraFinUtc
                        && t.Estado != "Cancelado")
            .ToListAsync();

        foreach (var turno in turnosSuperpuestos)
        {
            turno.Estado = "Cancelado";
            
            // Simular envío de email al paciente
            string emailDestinatario = turno.Paciente?.Email ?? "paciente@vitalis.local";
            string nombrePaciente = turno.Paciente != null ? $"{turno.Paciente.Nombre} {turno.Paciente.Apellido}" : "Paciente";
            string nombreMedico = $"{profesional.Nombre} {profesional.Apellido}";
            string fechaHoraStr = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            string asunto = "Cancelación de turno por fuerza mayor - Vitalis";
            string cuerpo = $@"<div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                <h2 style='color: #d9534f;'>Aviso de Cancelación de Turno</h2>
                <p>Estimado/a <strong>{nombrePaciente}</strong>,</p>
                <p>Lamentamos informarle que su turno programado con el profesional <strong>{nombreMedico}</strong> para el día <strong>{fechaHoraStr}</strong> ha sido cancelado debido a un bloqueo de agenda (Motivo: <em>{dto.Motivo}</em>).</p>
                <p>Por favor, ingrese al portal o póngase en contacto con recepción para reprogramar su cita.</p>
                <br/>
                <p>Disculpe las molestias ocasionadas.</p>
                <p>Atentamente,<br/><strong>Equipo Vitalis</strong></p>
            </div>";

            await _emailService.SendEmailAsync(emailDestinatario, asunto, cuerpo);
        }

        if (turnosSuperpuestos.Any())
        {
            await _context.SaveChangesAsync();
        }

        return await ObtenerPorIdAsync(bloqueo.Id) ?? throw new Exception("Error al crear bloqueo");
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var b = await _context.BloqueosAgenda.FindAsync(id);
        if (b == null) return false;

        _context.BloqueosAgenda.Remove(b);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EsHorarioBloqueadoAsync(int profesionalId, DateTime fechaHora)
    {
        var turnoFin = fechaHora.AddMinutes(30);

        return await _context.BloqueosAgenda
            .AnyAsync(b => b.ProfesionalId == profesionalId
                           && b.FechaHoraInicio < turnoFin
                           && b.FechaHoraFin > fechaHora);
    }
}
