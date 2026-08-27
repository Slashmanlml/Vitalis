using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Consultas;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class ConsultaMedicaService : IConsultaMedicaService
{
    private readonly VitalisDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IUsuarioActual _usuarioActual;

    public ConsultaMedicaService(
        VitalisDbContext context,
        IEmailService emailService,
        IUsuarioActual usuarioActual)
    {
        _context = context;
        _emailService = emailService;
        _usuarioActual = usuarioActual;
    }

    /// <summary>
    /// Un medico solo puede operar sobre los turnos que tiene asignados. Para el
    /// resto de los roles (administrador) no se restringe.
    /// </summary>
    private async Task VerificarQuePuedeOperarSobreAsync(int profesionalDelTurnoId)
    {
        if (!_usuarioActual.EsMedico)
        {
            return;
        }

        var miProfesionalId = await _usuarioActual.ObtenerProfesionalIdAsync();

        if (miProfesionalId is null)
        {
            throw new ForbiddenException(
                "Su usuario no esta vinculado a una ficha profesional, por lo que no puede registrar atenciones.");
        }

        if (miProfesionalId != profesionalDelTurnoId)
        {
            throw new ForbiddenException(
                "El turno pertenece a otro profesional. Solo el medico asignado puede registrar la atencion.");
        }
    }

    public async Task<List<ConsultaMedicaDto>> ObtenerPorPacienteAsync(int pacienteId)
    {
        return await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Profesional)
            .Where(c => c.PacienteId == pacienteId)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new ConsultaMedicaDto
            {
                Id = c.Id,
                PacienteId = c.PacienteId,
                PacienteNombre = c.Paciente.Nombre + " " + c.Paciente.Apellido,
                ProfesionalId = c.ProfesionalId,
                ProfesionalNombre = c.Profesional.Nombre + " " + c.Profesional.Apellido,
                TurnoId = c.TurnoId,
                Fecha = c.Fecha,
                MotivoConsulta = c.MotivoConsulta,
                Diagnostico = c.Diagnostico,
                Evolucion = c.Evolucion,
                Indicaciones = c.Indicaciones,
                Observaciones = c.Observaciones,
                EstudioAdjuntoUrl = c.EstudioAdjuntoUrl
            })
            .ToListAsync();
    }

    public async Task<ConsultaMedicaDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Profesional)
            .Where(c => c.Id == id)
            .Select(c => new ConsultaMedicaDto
            {
                Id = c.Id,
                PacienteId = c.PacienteId,
                PacienteNombre = c.Paciente.Nombre + " " + c.Paciente.Apellido,
                ProfesionalId = c.ProfesionalId,
                ProfesionalNombre = c.Profesional.Nombre + " " + c.Profesional.Apellido,
                TurnoId = c.TurnoId,
                Fecha = c.Fecha,
                MotivoConsulta = c.MotivoConsulta,
                Diagnostico = c.Diagnostico,
                Evolucion = c.Evolucion,
                Indicaciones = c.Indicaciones,
                Observaciones = c.Observaciones,
                EstudioAdjuntoUrl = c.EstudioAdjuntoUrl
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ConsultaMedicaDto> CrearAsync(CrearConsultaDto dto)
    {
        // TurnoId/PacienteId/ProfesionalId son FKs requeridas en la entidad ConsultaMedica
        // (navegaciones no-nulas): antes de este chequeo el servicio no validaba que
        // existieran, lo que permitía crear consultas "huérfanas" apuntando a ids
        // inexistentes. Ver hallazgo en task.md (semana 3).
        var turno = await _context.Turnos.FindAsync(dto.TurnoId)
            ?? throw new NotFoundException("Turno no encontrado.");

        // Un medico solo registra atenciones sobre sus propios turnos.
        await VerificarQuePuedeOperarSobreAsync(turno.ProfesionalId);

        // El paciente y el profesional se toman DEL TURNO, no del cuerpo del
        // pedido. Antes venian del navegador, de modo que un cliente podia
        // guardar una consulta atribuida a un profesional o a un paciente que
        // no tenian nada que ver con el turno. El turno es la fuente de verdad.
        var consulta = new ConsultaMedica
        {
            PacienteId = turno.PacienteId,
            ProfesionalId = turno.ProfesionalId,
            TurnoId = turno.Id,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = dto.MotivoConsulta,
            Diagnostico = dto.Diagnostico,
            Evolucion = dto.Evolucion,
            Indicaciones = dto.Indicaciones,
            Observaciones = dto.Observaciones,
            EstudioAdjuntoUrl = dto.EstudioAdjuntoUrl
        };

        turno.Estado = "Atendido";

        _context.ConsultasMedicas.Add(consulta);
        await _context.SaveChangesAsync();

        var pac = await _context.Pacientes.FindAsync(consulta.PacienteId);
        var prof = await _context.Profesionales.FindAsync(consulta.ProfesionalId);
        if (pac != null && !string.IsNullOrWhiteSpace(pac.Email))
        {
            await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
            {
                Destinatario = pac.Email,
                Evento = Domain.Constants.EventoNotificacion.ResumenConsulta,
                TurnoId = consulta.TurnoId,
                Datos = new Dictionary<string, string>
                {
                    ["PacienteNombre"] = $"{pac.Nombre} {pac.Apellido}",
                    ["ProfesionalNombre"] = prof != null ? $"{prof.Nombre} {prof.Apellido}" : "Médico Tratante",
                    ["FechaHora"] = consulta.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    ["Indicaciones"] = string.IsNullOrWhiteSpace(consulta.Indicaciones)
                        ? "Seguir las pautas acordadas durante la consulta médica."
                        : consulta.Indicaciones
                }
            });
        }

        return await ObtenerPorIdAsync(consulta.Id) ?? throw new Exception("Error al crear consulta");
    }

    public async Task<ConsultaMedicaDto?> EditarAsync(int id, EditarConsultaDto dto)
    {
        var consulta = await _context.ConsultasMedicas.FindAsync(id);
        if (consulta == null) return null;

        // Editar la historia clinica de un paciente ajeno es tan grave como crearla.
        await VerificarQuePuedeOperarSobreAsync(consulta.ProfesionalId);

        consulta.MotivoConsulta = dto.MotivoConsulta;
        consulta.Diagnostico = dto.Diagnostico;
        consulta.Evolucion = dto.Evolucion;
        consulta.Indicaciones = dto.Indicaciones;
        consulta.Observaciones = dto.Observaciones;
        consulta.EstudioAdjuntoUrl = dto.EstudioAdjuntoUrl;

        await _context.SaveChangesAsync();
        return await ObtenerPorIdAsync(id);
    }

    public async Task<List<AntecedenteDto>> ObtenerAntecedentesAsync(int pacienteId)
    {
        return await _context.AntecedentesClinicos
            .Where(a => a.PacienteId == pacienteId)
            .OrderByDescending(a => a.FechaRegistro)
            .Select(a => new AntecedenteDto
            {
                Id = a.Id,
                PacienteId = a.PacienteId,
                Tipo = a.Tipo,
                Descripcion = a.Descripcion,
                FechaRegistro = a.FechaRegistro
            })
            .ToListAsync();
    }

    public async Task<AntecedenteDto> CrearAntecedenteAsync(CrearAntecedenteDto dto)
    {
        var ant = new AntecedenteClinico
        {
            PacienteId = dto.PacienteId,
            Tipo = dto.Tipo,
            Descripcion = dto.Descripcion,
            FechaRegistro = DateTime.UtcNow
        };
        _context.AntecedentesClinicos.Add(ant);
        await _context.SaveChangesAsync();
        return new AntecedenteDto
        {
            Id = ant.Id,
            PacienteId = ant.PacienteId,
            Tipo = ant.Tipo,
            Descripcion = ant.Descripcion,
            FechaRegistro = ant.FechaRegistro
        };
    }

    public async Task<List<AlergiaDto>> ObtenerAlergiasAsync(int pacienteId)
    {
        return await _context.Alergias
            .Where(a => a.PacienteId == pacienteId && a.Activa)
            .Select(a => new AlergiaDto
            {
                Id = a.Id,
                PacienteId = a.PacienteId,
                Sustancia = a.Sustancia,
                Reaccion = a.Reaccion,
                Severidad = a.Severidad,
                Activa = a.Activa
            })
            .ToListAsync();
    }

    public async Task<AlergiaDto> CrearAlergiaAsync(CrearAlergiaDto dto)
    {
        var al = new Alergia
        {
            PacienteId = dto.PacienteId,
            Sustancia = dto.Sustancia,
            Reaccion = dto.Reaccion,
            Severidad = dto.Severidad,
            Activa = true
        };
        _context.Alergias.Add(al);
        await _context.SaveChangesAsync();
        return new AlergiaDto
        {
            Id = al.Id,
            PacienteId = al.PacienteId,
            Sustancia = al.Sustancia,
            Reaccion = al.Reaccion,
            Severidad = al.Severidad,
            Activa = al.Activa
        };
    }
}
