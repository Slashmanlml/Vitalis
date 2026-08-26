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

    /// <summary>
    /// Turnos que un bloqueo dejaría fuera de juego.
    ///
    /// La usan tanto la previsualización como la cancelación real, a propósito:
    /// si cada una tuviera su propia consulta, bastaría con tocar una para que el
    /// número anunciado al usuario dejara de coincidir con lo que efectivamente
    /// se cancela. Con una sola definición, eso no puede pasar.
    /// </summary>
    private IQueryable<Turno> TurnosAfectadosPor(int profesionalId, DateTime desdeUtc, DateTime hastaUtc)
    {
        return _context.Turnos
            .Where(t => t.ProfesionalId == profesionalId
                        && t.FechaHora >= desdeUtc
                        && t.FechaHora <= hastaUtc
                        && t.Estado != "Cancelado");
    }

    public async Task<ImpactoBloqueoDto> ObtenerImpactoAsync(int profesionalId, DateTime desde, DateTime hasta)
    {
        var desdeUtc = DateTime.SpecifyKind(desde, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta, DateTimeKind.Utc);

        if (desdeUtc >= hastaUtc)
        {
            throw new ValidationException("La fecha de inicio debe ser anterior a la de fin.");
        }

        var turnos = await TurnosAfectadosPor(profesionalId, desdeUtc, hastaUtc)
            .Include(t => t.Paciente)
            .OrderBy(t => t.FechaHora)
            .Select(t => new TurnoAfectadoDto
            {
                TurnoId = t.Id,
                FechaHora = t.FechaHora,
                PacienteNombre = t.Paciente!.Nombre + " " + t.Paciente.Apellido,
                Estado = t.Estado,
                TieneEmail = t.Paciente.Email != null && t.Paciente.Email != ""
            })
            .ToListAsync();

        return new ImpactoBloqueoDto
        {
            CantidadTurnos = turnos.Count,
            PacientesAfectados = turnos.Select(t => t.PacienteNombre).Distinct().Count(),
            PacientesConEmail = turnos.Where(t => t.TieneEmail)
                                      .Select(t => t.PacienteNombre).Distinct().Count(),
            Turnos = turnos
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
        var turnosSuperpuestos = await TurnosAfectadosPor(dto.ProfesionalId, fechaHoraInicioUtc, fechaHoraFinUtc)
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .ToListAsync();

        foreach (var turno in turnosSuperpuestos)
        {
            turno.Estado = "Cancelado";
            
            if (turno.Paciente != null && !string.IsNullOrWhiteSpace(turno.Paciente.Email))
            {
                await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
                {
                    Destinatario = turno.Paciente.Email,
                    Evento = Domain.Constants.EventoNotificacion.TurnoCancelado,
                    TurnoId = turno.Id,
                    Datos = new Dictionary<string, string>
                    {
                        ["PacienteNombre"] = $"{turno.Paciente.Nombre} {turno.Paciente.Apellido}",
                        ["ProfesionalNombre"] = $"{profesional.Nombre} {profesional.Apellido}",
                        ["FechaHora"] = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        ["Motivo"] = $"Fuerza mayor / Bloqueo de agenda: {dto.Motivo}"
                    }
                });
            }
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
