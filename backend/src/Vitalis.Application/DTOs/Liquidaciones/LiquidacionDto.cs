namespace Vitalis.Application.DTOs.Liquidaciones;

public class LiquidacionDto
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class CrearLiquidacionDto
{
    public int ProfesionalId { get; set; }
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
}
