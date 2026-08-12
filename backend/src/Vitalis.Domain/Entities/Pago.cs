namespace Vitalis.Domain.Entities;

public class Pago
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public Factura Factura { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public string MedioPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string? Observaciones { get; set; }
}
