namespace Vitalis.Domain.Entities;

public class Medicamento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Presentacion { get; set; }
    public bool Activo { get; set; } = true;
}
