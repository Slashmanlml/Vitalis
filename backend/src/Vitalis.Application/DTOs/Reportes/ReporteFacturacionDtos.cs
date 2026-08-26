namespace Vitalis.Application.DTOs.Reportes;

public class ReporteFacturacionPorPeriodoDto
{
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal TotalFacturado { get; set; }
    public int CantidadFacturas { get; set; }
    public decimal PromedioPorFactura { get; set; }
    public List<FacturacionPorObraSocialItemDto> PorObraSocial { get; set; } = new();
}

public class FacturacionPorObraSocialItemDto
{
    public int? ObraSocialId { get; set; }
    public string ObraSocialNombre { get; set; } = string.Empty;
    public decimal TotalFacturado { get; set; }
    public int CantidadFacturas { get; set; }
    public double PorcentajeDelTotal { get; set; }
}

public class ReporteCobranzasDto
{
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal TotalFacturado { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public double TasaCobranzaPorcentaje { get; set; }
    public int CantidadPagos { get; set; }
    public List<CobranzaPorMedioPagoItemDto> PorMedioPago { get; set; } = new();
}

public class CobranzaPorMedioPagoItemDto
{
    public string MedioPago { get; set; } = string.Empty;
    public decimal TotalCobrado { get; set; }
    public int CantidadPagos { get; set; }
    public double PorcentajeDelTotal { get; set; }
}

public class ReporteLiquidacionesPorPeriodoDto
{
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal TotalLiquidado { get; set; }
    public int CantidadLiquidaciones { get; set; }
    public List<LiquidacionProfesionalItemDto> PorProfesional { get; set; } = new();
}

public class LiquidacionProfesionalItemDto
{
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public decimal TotalLiquidado { get; set; }
    public int CantidadLiquidaciones { get; set; }
    public string Estado { get; set; } = string.Empty;
    public double PorcentajeDelTotal { get; set; }
}

public class ResumenFinancieroDto
{
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal TotalFacturado { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public decimal TotalLiquidado { get; set; }
    public decimal MargenBruto { get; set; }
    public double TasaCobranzaPorcentaje { get; set; }
    public List<FacturacionPorObraSocialItemDto> TopObrasSociales { get; set; } = new();
    public List<CobranzaPorMedioPagoItemDto> MediosPago { get; set; } = new();
    public List<LiquidacionProfesionalItemDto> TopLiquidacionesProfesionales { get; set; } = new();
}
