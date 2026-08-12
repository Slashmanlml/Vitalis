namespace Vitalis.Application.DTOs.Consultas;

public class ConsultaMedicaDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public int TurnoId { get; set; }
    public DateTime Fecha { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public string? Evolucion { get; set; }
    public string? Indicaciones { get; set; }
    public string? Observaciones { get; set; }
    public string? EstudioAdjuntoUrl { get; set; }
}

public class CrearConsultaDto
{
    public int TurnoId { get; set; }
    public int PacienteId { get; set; }
    public int ProfesionalId { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public string? Evolucion { get; set; }
    public string? Indicaciones { get; set; }
    public string? Observaciones { get; set; }
    public string? EstudioAdjuntoUrl { get; set; }
}

public class EditarConsultaDto
{
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public string? Evolucion { get; set; }
    public string? Indicaciones { get; set; }
    public string? Observaciones { get; set; }
    public string? EstudioAdjuntoUrl { get; set; }
}
