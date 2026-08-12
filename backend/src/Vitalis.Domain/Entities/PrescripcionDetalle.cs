namespace Vitalis.Domain.Entities;

public class PrescripcionDetalle
{
    public int Id { get; set; }
    public int PrescripcionId { get; set; }
    public Prescripcion Prescripcion { get; set; } = null!;
    public int MedicamentoId { get; set; }
    public Medicamento Medicamento { get; set; } = null!;
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;
    public string? Indicaciones { get; set; }
}
