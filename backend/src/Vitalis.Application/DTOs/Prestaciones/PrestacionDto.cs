namespace Vitalis.Application.DTOs.Prestaciones;

public class PrestacionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal ImporteBase { get; set; }
    public bool Activa { get; set; }
}

public class CrearPrestacionDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal ImporteBase { get; set; }
}

public class EditarPrestacionDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal ImporteBase { get; set; }
    public bool Activa { get; set; }
}
