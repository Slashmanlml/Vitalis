namespace Vitalis.Application.DTOs.Medicamentos;

public class MedicamentoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Presentacion { get; set; }
    public bool Activo { get; set; }
}

public class CrearMedicamentoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Presentacion { get; set; }
}

public class EditarMedicamentoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Presentacion { get; set; }
    public bool Activo { get; set; }
}
