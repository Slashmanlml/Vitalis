using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Reportes;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class ReporteFacturacionService : IReporteFacturacionService
{
    private readonly VitalisDbContext _context;

    public ReporteFacturacionService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task<ReporteFacturacionPorPeriodoDto> ObtenerFacturacionPorPeriodoAsync(DateTime desde, DateTime hasta)
    {
        var (desdeUtc, hastaUtc) = NormalizarRangoFechas(desde, hasta);

        var facturas = await _context.Facturas
            .Include(f => f.Paciente)
                .ThenInclude(p => p.ObraSocial)
            .Where(f => f.Fecha >= desdeUtc && f.Fecha <= hastaUtc)
            .ToListAsync();

        decimal totalFacturado = facturas.Sum(f => f.Total);
        int cantidadFacturas = facturas.Count;
        decimal promedio = cantidadFacturas > 0 ? Math.Round(totalFacturado / cantidadFacturas, 2) : 0m;

        var porObraSocial = facturas
            .GroupBy(f => new
            {
                Id = f.Paciente?.ObraSocialId,
                Nombre = f.Paciente?.ObraSocial != null ? f.Paciente.ObraSocial.Nombre : "Particular / Sin Obra Social"
            })
            .Select(g =>
            {
                decimal subtotal = g.Sum(f => f.Total);
                double porcentaje = totalFacturado > 0 
                    ? (double)Math.Round((subtotal / totalFacturado) * 100, 2) 
                    : 0;

                return new FacturacionPorObraSocialItemDto
                {
                    ObraSocialId = g.Key.Id,
                    ObraSocialNombre = g.Key.Nombre,
                    TotalFacturado = subtotal,
                    CantidadFacturas = g.Count(),
                    PorcentajeDelTotal = porcentaje
                };
            })
            .OrderByDescending(x => x.TotalFacturado)
            .ToList();

        return new ReporteFacturacionPorPeriodoDto
        {
            PeriodoDesde = desdeUtc,
            PeriodoHasta = hastaUtc,
            TotalFacturado = totalFacturado,
            CantidadFacturas = cantidadFacturas,
            PromedioPorFactura = promedio,
            PorObraSocial = porObraSocial
        };
    }

    public async Task<ReporteCobranzasDto> ObtenerCobranzasAsync(DateTime desde, DateTime hasta)
    {
        var (desdeUtc, hastaUtc) = NormalizarRangoFechas(desde, hasta);

        var facturas = await _context.Facturas
            .Where(f => f.Fecha >= desdeUtc && f.Fecha <= hastaUtc)
            .ToListAsync();

        decimal totalFacturado = facturas.Sum(f => f.Total);

        var pagos = await _context.Pagos
            .Where(p => p.Fecha >= desdeUtc && p.Fecha <= hastaUtc)
            .ToListAsync();

        decimal totalCobrado = pagos.Sum(p => p.Importe);
        decimal saldoPendiente = Math.Max(0m, totalFacturado - totalCobrado);
        
        double tasaCobranza = totalFacturado > 0
            ? Math.Min(100.0, (double)Math.Round((totalCobrado / totalFacturado) * 100, 2))
            : (totalCobrado > 0 ? 100.0 : 0.0);

        var porMedioPago = pagos
            .GroupBy(p => string.IsNullOrWhiteSpace(p.MedioPago) ? "Efectivo / No especificado" : p.MedioPago)
            .Select(g =>
            {
                decimal subtotal = g.Sum(p => p.Importe);
                double porcentaje = totalCobrado > 0
                    ? (double)Math.Round((subtotal / totalCobrado) * 100, 2)
                    : 0;

                return new CobranzaPorMedioPagoItemDto
                {
                    MedioPago = g.Key,
                    TotalCobrado = subtotal,
                    CantidadPagos = g.Count(),
                    PorcentajeDelTotal = porcentaje
                };
            })
            .OrderByDescending(x => x.TotalCobrado)
            .ToList();

        return new ReporteCobranzasDto
        {
            PeriodoDesde = desdeUtc,
            PeriodoHasta = hastaUtc,
            TotalFacturado = totalFacturado,
            TotalCobrado = totalCobrado,
            SaldoPendiente = saldoPendiente,
            TasaCobranzaPorcentaje = tasaCobranza,
            CantidadPagos = pagos.Count,
            PorMedioPago = porMedioPago
        };
    }

    public async Task<ReporteLiquidacionesPorPeriodoDto> ObtenerLiquidacionesPorPeriodoAsync(DateTime desde, DateTime hasta)
    {
        var (desdeUtc, hastaUtc) = NormalizarRangoFechas(desde, hasta);

        var liquidaciones = await _context.Liquidaciones
            .Include(l => l.Profesional)
                .ThenInclude(p => p.Especialidad)
            .Where(l => (l.FechaCreacion >= desdeUtc && l.FechaCreacion <= hastaUtc)
                     || (l.PeriodoDesde >= desdeUtc && l.PeriodoHasta <= hastaUtc))
            .ToListAsync();

        decimal totalLiquidado = liquidaciones.Sum(l => l.Total);
        int cantidadLiquidaciones = liquidaciones.Count;

        var porProfesional = liquidaciones
            .GroupBy(l => new
            {
                l.ProfesionalId,
                Nombre = l.Profesional != null ? $"{l.Profesional.Nombre} {l.Profesional.Apellido}" : "Médico Desconocido",
                Especialidad = l.Profesional?.Especialidad?.Nombre ?? "General"
            })
            .Select(g =>
            {
                decimal subtotal = g.Sum(l => l.Total);
                double porcentaje = totalLiquidado > 0
                    ? (double)Math.Round((subtotal / totalLiquidado) * 100, 2)
                    : 0;

                // Estado más frecuente
                string estadoPredominante = g
                    .GroupBy(l => l.Estado)
                    .OrderByDescending(eg => eg.Count())
                    .Select(eg => eg.Key)
                    .FirstOrDefault() ?? "Pendiente";

                return new LiquidacionProfesionalItemDto
                {
                    ProfesionalId = g.Key.ProfesionalId,
                    ProfesionalNombre = g.Key.Nombre,
                    Especialidad = g.Key.Especialidad,
                    TotalLiquidado = subtotal,
                    CantidadLiquidaciones = g.Count(),
                    Estado = estadoPredominante,
                    PorcentajeDelTotal = porcentaje
                };
            })
            .OrderByDescending(x => x.TotalLiquidado)
            .ToList();

        return new ReporteLiquidacionesPorPeriodoDto
        {
            PeriodoDesde = desdeUtc,
            PeriodoHasta = hastaUtc,
            TotalLiquidado = totalLiquidado,
            CantidadLiquidaciones = cantidadLiquidaciones,
            PorProfesional = porProfesional
        };
    }

    public async Task<ResumenFinancieroDto> ObtenerResumenFinancieroAsync(DateTime desde, DateTime hasta)
    {
        var facturacion = await ObtenerFacturacionPorPeriodoAsync(desde, hasta);
        var cobranzas = await ObtenerCobranzasAsync(desde, hasta);
        var liquidaciones = await ObtenerLiquidacionesPorPeriodoAsync(desde, hasta);

        decimal margenBruto = facturacion.TotalFacturado - liquidaciones.TotalLiquidado;

        return new ResumenFinancieroDto
        {
            PeriodoDesde = facturacion.PeriodoDesde,
            PeriodoHasta = facturacion.PeriodoHasta,
            TotalFacturado = facturacion.TotalFacturado,
            TotalCobrado = cobranzas.TotalCobrado,
            SaldoPendiente = cobranzas.SaldoPendiente,
            TotalLiquidado = liquidaciones.TotalLiquidado,
            MargenBruto = margenBruto,
            TasaCobranzaPorcentaje = cobranzas.TasaCobranzaPorcentaje,
            TopObrasSociales = facturacion.PorObraSocial.Take(5).ToList(),
            MediosPago = cobranzas.PorMedioPago,
            TopLiquidacionesProfesionales = liquidaciones.PorProfesional.Take(5).ToList()
        };
    }

    private static (DateTime DesdeUtc, DateTime HastaUtc) NormalizarRangoFechas(DateTime desde, DateTime hasta)
    {
        var d = DateTime.SpecifyKind(desde.Date, DateTimeKind.Utc);
        var h = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        return (d, h);
    }
}
