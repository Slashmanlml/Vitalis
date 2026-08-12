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
            ?? throw new Exception("Profesional no encontrado");

        var fechaDesdeInicioDia = dto.PeriodoDesde.Date;
        var fechaHastaFinDia = dto.PeriodoHasta.Date.AddDays(1).AddTicks(-1);

        var turnos = await _context.Turnos
            .Include(t => t.ObraSocial)
            .Where(t => t.ProfesionalId == dto.ProfesionalId &&
                        t.FechaHora >= fechaDesdeInicioDia &&
                        t.FechaHora <= fechaHastaFinDia &&
                        (t.Estado == "Atendido" || t.Estado == "En Consulta" || t.ConsultaMedica != null))
            .ToListAsync();

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
            PeriodoDesde = dto.PeriodoDesde,
            PeriodoHasta = dto.PeriodoHasta,
            Total = total,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        _context.Liquidaciones.Add(liquidacion);
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(liquidacion.Id) ?? throw new Exception("Error al crear liquidacion");
    }

    public async Task<LiquidacionDto?> LiquidarAsync(int id)
    {
        var liquidacion = await _context.Liquidaciones.FindAsync(id)
            ?? throw new Exception("Liquidacion no encontrada");

        liquidacion.Estado = "Liquidada";
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(id);
    }
}
