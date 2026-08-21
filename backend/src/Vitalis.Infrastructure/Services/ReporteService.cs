using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Turnos;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class ReporteService : IReporteService
{
    private readonly VitalisDbContext _context;

    public ReporteService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TurnoDto>> TurnosPorProfesionalAsync(int profesionalId, DateTime? desde, DateTime? hasta)
    {
        var query = _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .Include(t => t.ObraSocial)
            .Where(t => t.ProfesionalId == profesionalId);

        if (desde.HasValue) query = query.Where(t => t.FechaHora >= desde.Value);
        if (hasta.HasValue) query = query.Where(t => t.FechaHora <= hasta.Value);

        return await query.Select(t => new TurnoDto
        {
            Id = t.Id,
            PacienteId = t.PacienteId,
            PacienteNombre = t.Paciente.Nombre,
            ProfesionalId = t.ProfesionalId,
            ProfesionalNombre = t.Profesional.Nombre,
            ObraSocialId = t.ObraSocialId,
            ObraSocialNombre = t.ObraSocial.Nombre,
            FechaHora = t.FechaHora,
            Confirmado = t.Confirmado
        }).ToListAsync();
    }

    public async Task<IEnumerable<TurnoDto>> TurnosPorPacienteAsync(int pacienteId)
    {
        return await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .Include(t => t.ObraSocial)
            .Where(t => t.PacienteId == pacienteId)
            .Select(t => new TurnoDto
            {
                Id = t.Id,
                PacienteId = t.PacienteId,
                PacienteNombre = t.Paciente.Nombre,
                ProfesionalId = t.ProfesionalId,
                ProfesionalNombre = t.Profesional.Nombre,
                ObraSocialId = t.ObraSocialId,
                ObraSocialNombre = t.ObraSocial.Nombre,
                FechaHora = t.FechaHora,
                Confirmado = t.Confirmado
            }).ToListAsync();
    }

    public async Task<IEnumerable<TurnoDto>> TurnosPorObraSocialAsync(int obraSocialId)
    {
        return await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .Include(t => t.ObraSocial)
            .Where(t => t.ObraSocialId == obraSocialId)
            .Select(t => new TurnoDto
            {
                Id = t.Id,
                PacienteId = t.PacienteId,
                PacienteNombre = t.Paciente.Nombre,
                ProfesionalId = t.ProfesionalId,
                ProfesionalNombre = t.Profesional.Nombre,
                ObraSocialId = t.ObraSocialId,
                ObraSocialNombre = t.ObraSocial.Nombre,
                FechaHora = t.FechaHora,
                Confirmado = t.Confirmado
            }).ToListAsync();
    }

    public async Task<object> EstadisticasGeneralesAsync()
    {
        var totalTurnos = await _context.Turnos.CountAsync();
        var confirmados = await _context.Turnos.CountAsync(t => t.Confirmado);
        var pendientes = totalTurnos - confirmados;

        var porEspecialidad = await _context.Profesionales
            .GroupJoin(_context.Turnos,
                p => p.Id,
                t => t.ProfesionalId,
                (p, turnos) => new { p.Especialidad, Cantidad = turnos.Count() })
            .ToListAsync();

        return new
        {
            TotalTurnos = totalTurnos,
            Confirmados = confirmados,
            Pendientes = pendientes,
            PorEspecialidad = porEspecialidad
        };
    }
}
