namespace Vitalis.Domain.Entities;

public class Prestacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal ImporteBase { get; set; }
    public bool Activa { get; set; } = true;
}
