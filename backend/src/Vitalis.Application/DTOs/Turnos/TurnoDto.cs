namespace Vitalis.Application.DTOs.Turnos;

public class TurnoDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = string.Empty;
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public int ObraSocialId { get; set; }
    public string ObraSocialNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public bool Confirmado { get; set; }
    public string Estado { get; set; } = "Solicitado";
}
