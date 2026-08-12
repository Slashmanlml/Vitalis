namespace Vitalis.Domain.Entities;

public class Turno
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }

    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }

    public int PacienteId { get; set; }
    public Paciente? Paciente { get; set; }

    public int ObraSocialId { get; set; }
    public ObraSocial? ObraSocial { get; set; }

    public bool Confirmado { get; set; }

    public string Estado { get; set; } = "Solicitado";

    public ConsultaMedica? ConsultaMedica { get; set; }
}
