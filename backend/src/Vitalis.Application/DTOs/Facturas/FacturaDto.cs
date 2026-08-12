namespace Vitalis.Application.DTOs.Facturas;

public class FacturaDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<FacturaDetalleDto> Detalles { get; set; } = new();
    public List<PagoDto> Pagos { get; set; } = new();
}

public class FacturaDetalleDto
{
    public int Id { get; set; }
    public int PrestacionId { get; set; }
    public string PrestacionNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class PagoDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string MedioPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string? Observaciones { get; set; }
}

public class CrearFacturaDto
{
    public int PacienteId { get; set; }
    public string? Observaciones { get; set; }
    public List<CrearFacturaDetalleDto> Detalles { get; set; } = new();
}

public class CrearFacturaDetalleDto
{
    public int PrestacionId { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
}

public class RegistrarPagoDto
{
    public int FacturaId { get; set; }
    public string MedioPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string? Observaciones { get; set; }
}
