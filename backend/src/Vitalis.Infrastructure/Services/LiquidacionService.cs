using Vitalis.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Liquidaciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class LiquidacionService : ILiquidacionService
{
    private readonly VitalisDbContext _context;

    public LiquidacionService(VitalisDbContext context) => _context = context;

    public async Task<List<LiquidacionDto>> ObtenerTodasAsync()
    {
        return await _context.Liquidaciones
            .Include(l => l.Profesional)
            .OrderByDescending(l => l.FechaCreacion)
            .Select(l => new LiquidacionDto
            {
                Id = l.Id,
                ProfesionalId = l.ProfesionalId,
                ProfesionalNombre = l.Profesional.Nombre + " " + l.Profesional.Apellido,
                PeriodoDesde = l.PeriodoDesde,
                PeriodoHasta = l.PeriodoHasta,
                Total = l.Total,
                Estado = l.Estado,
                FechaCreacion = l.FechaCreacion
            })
            .ToListAsync();
    }

    public async Task<LiquidacionDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Liquidaciones
            .Include(l => l.Profesional)
            .Where(l => l.Id == id)
            .Select(l => new LiquidacionDto
            {
                Id = l.Id,
                ProfesionalId = l.ProfesionalId,
                ProfesionalNombre = l.Profesional.Nombre + " " + l.Profesional.Apellido,
                PeriodoDesde = l.PeriodoDesde,
                PeriodoHasta = l.PeriodoHasta,
                Total = l.Total,
                Estado = l.Estado,
                FechaCreacion = l.FechaCreacion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<LiquidacionDto> CrearAsync(CrearLiquidacionDto dto)
    {
        var profesional = await _context.Profesionales.FindAsync(dto.ProfesionalId)
            ?? throw new NotFoundException("Profesional no encontrado");

        if (dto.PeriodoDesde.Date > dto.PeriodoHasta.Date)
        {
            throw new ValidationException("La fecha de inicio del período debe ser anterior o igual a la de fin.");
        }

        // Se normaliza a Utc antes de usarla en la consulta: Npgsql rechaza
        // DateTimeKind.Unspecified como parámetro de consulta.
        var fechaDesdeInicioDia = DateTime.SpecifyKind(dto.PeriodoDesde.Date, DateTimeKind.Utc);
        var fechaHastaFinDia = DateTime.SpecifyKind(dto.PeriodoHasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var turnos = await _context.Turnos
            .Include(t => t.ObraSocial)
            .Where(t => t.ProfesionalId == dto.ProfesionalId &&
                        t.FechaHora >= fechaDesdeInicioDia &&
                        t.FechaHora <= fechaHastaFinDia &&
                        (t.Estado == "Atendido" || t.Estado == "En Consulta" || t.ConsultaMedica != null))
            .ToListAsync();

        if (turnos.Count == 0)
        {
            throw new ValidationException("No hay turnos atendidos de este profesional en ese período: no hay nada para liquidar.");
        }

        // Liquidar dos veces el mismo período le pagaría dos veces los mismos
        // turnos al profesional. Se rechaza cualquier superposición.
        var desdeUtc = DateTime.SpecifyKind(dto.PeriodoDesde.Date, DateTimeKind.Utc);
        var haySuperposicion = await _context.Liquidaciones.AnyAsync(l =>
            l.ProfesionalId == dto.ProfesionalId &&
            l.PeriodoDesde <= fechaHastaFinDia &&
            l.PeriodoHasta >= desdeUtc);
        if (haySuperposicion)
        {
            throw new ConflictException("Ya hay una liquidación de este profesional que cubre parte de ese período.");
        }

        decimal total = 0;
        foreach (var t in turnos)
        {
            var codigo = t.ObraSocial?.Codigo;
            decimal honorario = 0;
            switch (codigo)
            {
                case "OSDE":
                case "SM":
                case "GAL":
                    // Base rate $3200, 80% to doctor
                    honorario = 3200m * 0.80m;
                    break;
                case "OSECAC":
                case "IOMA":
                    // Base rate $2400, 75% to doctor
                    honorario = 2400m * 0.75m;
                    break;
                case "PAMI":
                    // Base rate $2000, 70% to doctor
                    honorario = 2000m * 0.70m;
                    break;
                default:
                    // Particular: base rate $4000, 90% to doctor
                    honorario = 4000m * 0.90m;
                    break;
            }
            total += honorario;
        }

        var liquidacion = new Liquidacion
        {
            ProfesionalId = dto.ProfesionalId,
            // PostgreSQL exige DateTimeKind.Utc para timestamptz.
            PeriodoDesde = DateTime.SpecifyKind(dto.PeriodoDesde, DateTimeKind.Utc),
            PeriodoHasta = DateTime.SpecifyKind(dto.PeriodoHasta, DateTimeKind.Utc),
            Total = total,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        _context.Liquidaciones.Add(liquidacion);
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(liquidacion.Id) ?? throw new InvalidOperationException("No se pudo recuperar la liquidación recién creada.");
    }

    public async Task<LiquidacionDto?> LiquidarAsync(int id)
    {
        var liquidacion = await _context.Liquidaciones.FindAsync(id);
        if (liquidacion == null) return null;

        if (liquidacion.Estado == "Liquidada")
        {
            throw new ConflictException("Esta liquidación ya fue marcada como liquidada.");
        }

        liquidacion.Estado = "Liquidada";
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(id);
    }
}
