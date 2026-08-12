namespace Vitalis.Domain.Entities;

public class ConsultaMedica
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int ProfesionalId { get; set; }
    public Profesional Profesional { get; set; } = null!;
    public int TurnoId { get; set; }
    public Turno Turno { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? Diagnostico { get; set; }
    public string? Evolucion { get; set; }
    public string? Indicaciones { get; set; }
    public string? Observaciones { get; set; }
    public string? EstudioAdjuntoUrl { get; set; }
}
