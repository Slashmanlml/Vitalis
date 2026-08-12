namespace Vitalis.Application.DTOs.Prescripciones;

public class PrescripcionDto
{
    public int Id { get; set; }
    public int ConsultaMedicaId { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
    public List<PrescripcionDetalleDto> Detalles { get; set; } = new();
}

public class PrescripcionDetalleDto
{
    public int Id { get; set; }
    public int MedicamentoId { get; set; }
    public string MedicamentoNombre { get; set; } = string.Empty;
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;
    public string? Indicaciones { get; set; }
}

public class CrearPrescripcionDto
{
    public int ConsultaMedicaId { get; set; }
    public int PacienteId { get; set; }
    public int ProfesionalId { get; set; }
    public string? Observaciones { get; set; }
    public List<CrearPrescripcionDetalleDto> Detalles { get; set; } = new();
}

public class CrearPrescripcionDetalleDto
{
    public int MedicamentoId { get; set; }
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;
    public string? Indicaciones { get; set; }
}
