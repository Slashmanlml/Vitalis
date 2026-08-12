namespace Vitalis.Domain.Entities;

public class Liquidacion
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public Profesional Profesional { get; set; } = null!;
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; }
}
