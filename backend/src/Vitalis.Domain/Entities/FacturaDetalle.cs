namespace Vitalis.Domain.Entities;

public class FacturaDetalle
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public Factura Factura { get; set; } = null!;
    public int PrestacionId { get; set; }
    public Prestacion Prestacion { get; set; } = null!;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
