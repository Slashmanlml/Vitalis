using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Facturacion}")]
public class ReportesFacturacionController : ControllerBase
{
    private readonly IReporteFacturacionService _reporteService;

    public ReportesFacturacionController(IReporteFacturacionService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet("facturacion")]
    public async Task<IActionResult> ObtenerFacturacion([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var (d, h) = DeterminarRango(desde, hasta);
        var reporte = await _reporteService.ObtenerFacturacionPorPeriodoAsync(d, h);
        return Ok(reporte);
    }

    [HttpGet("cobranzas")]
    public async Task<IActionResult> ObtenerCobranzas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var (d, h) = DeterminarRango(desde, hasta);
        var reporte = await _reporteService.ObtenerCobranzasAsync(d, h);
        return Ok(reporte);
    }

    [HttpGet("liquidaciones")]
    public async Task<IActionResult> ObtenerLiquidaciones([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var (d, h) = DeterminarRango(desde, hasta);
        var reporte = await _reporteService.ObtenerLiquidacionesPorPeriodoAsync(d, h);
        return Ok(reporte);
    }

    [HttpGet("resumen-financiero")]
    public async Task<IActionResult> ObtenerResumenFinanciero([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var (d, h) = DeterminarRango(desde, hasta);
        var reporte = await _reporteService.ObtenerResumenFinancieroAsync(d, h);
        return Ok(reporte);
    }

    private static (DateTime Desde, DateTime Hasta) DeterminarRango(DateTime? desde, DateTime? hasta)
    {
        var h = hasta ?? DateTime.Today;
        var d = desde ?? new DateTime(h.Year, h.Month, 1);
        return (d, h);
    }
}
