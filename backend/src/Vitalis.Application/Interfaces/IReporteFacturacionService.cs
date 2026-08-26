using Vitalis.Application.DTOs.Reportes;

namespace Vitalis.Application.Interfaces;

public interface IReporteFacturacionService
{
    Task<ReporteFacturacionPorPeriodoDto> ObtenerFacturacionPorPeriodoAsync(DateTime desde, DateTime hasta);
    Task<ReporteCobranzasDto> ObtenerCobranzasAsync(DateTime desde, DateTime hasta);
    Task<ReporteLiquidacionesPorPeriodoDto> ObtenerLiquidacionesPorPeriodoAsync(DateTime desde, DateTime hasta);
    Task<ResumenFinancieroDto> ObtenerResumenFinancieroAsync(DateTime desde, DateTime hasta);
}
