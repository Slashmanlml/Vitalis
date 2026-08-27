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
    private readonly IUsuarioActual _usuarioActual;

    public TurnoService(VitalisDbContext context, IEmailService emailService, IUsuarioActual usuarioActual)
    {
        _context = context;
        _emailService = emailService;
        _usuarioActual = usuarioActual;
    }

    public async Task<IEnumerable<TurnoDto>> ObtenerTodosAsync()
    {
        var consulta = _context.Turnos.AsQueryable();

        // El filtrado por rol se hacia en el navegador: el backend enviaba la
        // agenda COMPLETA de la clinica y el frontend escondia lo ajeno con un
        // .filter(). Cualquiera que abriera las herramientas de desarrollo veia
        // los turnos y los nombres de pacientes de todos los profesionales.
        // Ahora los datos que no corresponden nunca salen del servidor.
        if (_usuarioActual.EsMedico)
        {
            var miProfesionalId = await _usuarioActual.ObtenerProfesionalIdAsync();

            // Sin ficha profesional vinculada no se devuelve nada: es preferible
            // una agenda vacia a filtrar la de la clinica entera.
            consulta = consulta.Where(t => miProfesionalId != null && t.ProfesionalId == miProfesionalId);
        }

        // Sin Include: al proyectar con Select, EF resuelve las navegaciones en el
        // propio JOIN y el Include queda ignorado (lo avisa por consola en cada
        // peticion).
        return await consulta
            .Select(t => new TurnoDto
            {
                Id = t.Id,
                PacienteId = t.PacienteId,
                PacienteNombre = t.Paciente!.Nombre + " " + t.Paciente!.Apellido,
                ProfesionalId = t.ProfesionalId,
                ProfesionalNombre = t.Profesional!.Nombre + " " + t.Profesional!.Apellido,
                ObraSocialId = t.ObraSocialId,
                ObraSocialNombre = t.ObraSocial!.Nombre,
                FechaHora = t.FechaHora,
                Confirmado = t.Confirmado,
                Estado = t.Estado
            })
            .OrderByDescending(t => t.FechaHora)
            .ToListAsync();
    }

    public async Task<TurnoDto?> ObtenerPorIdAsync(int id)
    {
        // Se proyecta en la consulta en vez de traer el turno entero y mapearlo
        // despues. Dos motivos: EF pide a la base solo las columnas que se usan,
        // y sobre todo, dentro de un Select el acceso a las navegaciones se
        // traduce a JOIN y nunca se ejecuta como C#, de modo que no puede haber
        // una desreferencia nula en tiempo de ejecucion. Antes esto materializaba
        // el turno y hacia turno.Paciente.Nombre: si alguien quitaba un Include,
        // reventaba recien en produccion.
        return await _context.Turnos
            .Where(t => t.Id == id)
            .Select(t => new TurnoDto
            {
                Id = t.Id,
                PacienteId = t.PacienteId,
                PacienteNombre = t.Paciente!.Nombre + " " + t.Paciente!.Apellido,
                ProfesionalId = t.ProfesionalId,
                ProfesionalNombre = t.Profesional!.Nombre + " " + t.Profesional!.Apellido,
                ObraSocialId = t.ObraSocialId,
                ObraSocialNombre = t.ObraSocial!.Nombre,
                FechaHora = t.FechaHora,
                Confirmado = t.Confirmado,
                Estado = t.Estado
            })
            .FirstOrDefaultAsync();
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
        var prof = await _context.Profesionales.Include(p => p.Especialidad).FirstOrDefaultAsync(p => p.Id == turno.ProfesionalId);
        if (pac != null && !string.IsNullOrWhiteSpace(pac.Email))
        {
            await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
            {
                Destinatario = pac.Email,
                Evento = Domain.Constants.EventoNotificacion.TurnoCreado,
                TurnoId = turno.Id,
                Datos = new Dictionary<string, string>
                {
                    ["PacienteNombre"] = $"{pac.Nombre} {pac.Apellido}",
                    ["ProfesionalNombre"] = prof != null ? $"{prof.Nombre} {prof.Apellido}" : "Médico Asignado",
                    ["Especialidad"] = prof?.Especialidad?.Nombre ?? "Medicina General",
                    ["FechaHora"] = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                }
            });
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
        var prof = await _context.Profesionales.Include(p => p.Especialidad).FirstOrDefaultAsync(p => p.Id == turno.ProfesionalId);

        // Notificar Reprogramación si cambió fecha y hora
        if (turno.FechaHora != fechaAnterior && pac != null && !string.IsNullOrWhiteSpace(pac.Email))
        {
            await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
            {
                Destinatario = pac.Email,
                Evento = Domain.Constants.EventoNotificacion.TurnoReprogramado,
                TurnoId = turno.Id,
                Datos = new Dictionary<string, string>
                {
                    ["PacienteNombre"] = $"{pac.Nombre} {pac.Apellido}",
                    ["ProfesionalNombre"] = prof != null ? $"{prof.Nombre} {prof.Apellido}" : "Médico Asignado",
                    ["Especialidad"] = prof?.Especialidad?.Nombre ?? "Medicina General",
                    ["FechaAnterior"] = fechaAnterior.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    ["FechaHora"] = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                }
            });
        }

        // Notificar Cancelación
        if (turno.Estado == "Cancelado" && estadoAnterior != "Cancelado" && pac != null && !string.IsNullOrWhiteSpace(pac.Email))
        {
            await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
            {
                Destinatario = pac.Email,
                Evento = Domain.Constants.EventoNotificacion.TurnoCancelado,
                TurnoId = turno.Id,
                Datos = new Dictionary<string, string>
                {
                    ["PacienteNombre"] = $"{pac.Nombre} {pac.Apellido}",
                    ["ProfesionalNombre"] = prof != null ? $"{prof.Nombre} {prof.Apellido}" : "Médico Asignado",
                    ["FechaHora"] = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                }
            });
        }

        // Notificar Confirmación (solo en transición false -> true)
        if (turno.Confirmado && !confirmadoAnterior && pac != null && !string.IsNullOrWhiteSpace(pac.Email))
        {
            await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
            {
                Destinatario = pac.Email,
                Evento = Domain.Constants.EventoNotificacion.TurnoConfirmado,
                TurnoId = turno.Id,
                Datos = new Dictionary<string, string>
                {
                    ["PacienteNombre"] = $"{pac.Nombre} {pac.Apellido}",
                    ["ProfesionalNombre"] = prof != null ? $"{prof.Nombre} {prof.Apellido}" : "Médico Asignado",
                    ["Especialidad"] = prof?.Especialidad?.Nombre ?? "Medicina General",
                    ["FechaHora"] = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                }
            });
        }

        return await ObtenerPorIdAsync(turno.Id);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return false;

        var pac = await _context.Pacientes.FindAsync(turno.PacienteId);
        var prof = await _context.Profesionales.FindAsync(turno.ProfesionalId);
        if (pac != null && !string.IsNullOrWhiteSpace(pac.Email))
        {
            await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
            {
                Destinatario = pac.Email,
                Evento = Domain.Constants.EventoNotificacion.TurnoCancelado,
                TurnoId = turno.Id,
                Datos = new Dictionary<string, string>
                {
                    ["PacienteNombre"] = $"{pac.Nombre} {pac.Apellido}",
                    ["ProfesionalNombre"] = prof != null ? $"{prof.Nombre} {prof.Apellido}" : "Médico Asignado",
                    ["FechaHora"] = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                }
            });
        }

        _context.Turnos.Remove(turno);
        await _context.SaveChangesAsync();
        return true;
    }
}
