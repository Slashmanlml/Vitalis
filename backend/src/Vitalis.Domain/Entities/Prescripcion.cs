namespace Vitalis.Domain.Entities;

public class Prescripcion
{
    public int Id { get; set; }
    public int ConsultaMedicaId { get; set; }
    public ConsultaMedica ConsultaMedica { get; set; } = null!;
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int ProfesionalId { get; set; }
    public Profesional Profesional { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
    public List<PrescripcionDetalle> Detalles { get; set; } = new();
}
