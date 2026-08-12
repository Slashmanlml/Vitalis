namespace Vitalis.Domain.Entities;

public class Factura
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
    public List<FacturaDetalle> Detalles { get; set; } = new();
    public List<Pago> Pagos { get; set; } = new();
}
