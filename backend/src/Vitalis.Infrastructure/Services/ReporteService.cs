using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Reportes;
using Vitalis.Application.DTOs.Turnos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class ReporteService : IReporteService
{
    private readonly VitalisDbContext _context;

    public ReporteService(VitalisDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Proyección única de Turno a TurnoDto, compartida por los tres reportes.
    /// Antes cada método repetía la suya y las tres tenían el mismo par de
    /// defectos: mostraban sólo el nombre de pila (sin apellido, a diferencia
    /// del resto del sistema) y nunca asignaban Estado, con lo cual todos los
    /// turnos de cualquier reporte salían como "Solicitado".
    /// </summary>
    private static readonly Expression<Func<Turno, TurnoDto>> ProyectarTurno = t => new TurnoDto
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
    };

    public async Task<IEnumerable<TurnoDto>> TurnosPorProfesionalAsync(int profesionalId, DateTime? desde, DateTime? hasta)
    {
        var query = _context.Turnos.Where(t => t.ProfesionalId == profesionalId);

        if (desde.HasValue) query = query.Where(t => t.FechaHora >= desde.Value);
        if (hasta.HasValue) query = query.Where(t => t.FechaHora <= hasta.Value);

        return await query
            .OrderByDescending(t => t.FechaHora)
            .Select(ProyectarTurno)
            .ToListAsync();
    }

    public async Task<IEnumerable<TurnoDto>> TurnosPorPacienteAsync(int pacienteId)
    {
        return await _context.Turnos
            .Where(t => t.PacienteId == pacienteId)
            .OrderByDescending(t => t.FechaHora)
            .Select(ProyectarTurno)
            .ToListAsync();
    }

    public async Task<IEnumerable<TurnoDto>> TurnosPorObraSocialAsync(int obraSocialId)
    {
        return await _context.Turnos
            .Where(t => t.ObraSocialId == obraSocialId)
            .OrderByDescending(t => t.FechaHora)
            .Select(ProyectarTurno)
            .ToListAsync();
    }

    public async Task<EstadisticasGeneralesDto> EstadisticasGeneralesAsync()
    {
        var total = await _context.Turnos.CountAsync();
        var confirmados = await _context.Turnos.CountAsync(t => t.Confirmado);
        var atendidos = await _context.Turnos.CountAsync(t => t.Estado == "Atendido");
        var cancelados = await _context.Turnos.CountAsync(t => t.Estado == "Cancelado");

        // Se agrupa desde Turnos y no desde Profesionales: la versión anterior
        // hacía un GroupJoin que emitía una fila POR PROFESIONAL etiquetada con
        // su especialidad, de modo que dos cardiólogos generaban dos filas
        // "Cardiología" en lugar de sumarse en una sola.
        var porEspecialidad = await _context.Turnos
            .GroupBy(t => t.Profesional.Especialidad!.Nombre)
            .Select(g => new ConteoPorCategoriaDto { Etiqueta = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var porObraSocial = await _context.Turnos
            .GroupBy(t => t.ObraSocial.Nombre)
            .Select(g => new ConteoPorCategoriaDto { Etiqueta = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var porProfesional = await _context.Turnos
            .GroupBy(t => t.Profesional.Nombre + " " + t.Profesional.Apellido)
            .Select(g => new ConteoPorCategoriaDto { Etiqueta = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        // El armado de la etiqueta "AAAA-MM" se hace en memoria: no todos los
        // proveedores traducen el formateo de cadenas a SQL.
        var porMesCrudo = await _context.Turnos
            .GroupBy(t => new { Anio = t.FechaHora.Year, Mes = t.FechaHora.Month })
            .Select(g => new { g.Key.Anio, g.Key.Mes, Cantidad = g.Count() })
            .ToListAsync();

        return new EstadisticasGeneralesDto
        {
            TotalTurnos = total,
            Confirmados = confirmados,
            Pendientes = total - confirmados,
            Atendidos = atendidos,
            Cancelados = cancelados,
            PorEspecialidad = porEspecialidad.OrderByDescending(x => x.Cantidad).ToList(),
            PorObraSocial = porObraSocial.OrderByDescending(x => x.Cantidad).ToList(),
            PorProfesional = porProfesional.OrderByDescending(x => x.Cantidad).ToList(),
            PorMes = porMesCrudo
                .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                .Select(x => new ConteoPorCategoriaDto
                {
                    Etiqueta = $"{x.Anio:0000}-{x.Mes:00}",
                    Cantidad = x.Cantidad
                })
                .ToList()
        };
    }
}
