namespace Vitalis.Application.DTOs.Turnos;

public class CrearTurnoDto
{
    public int PacienteId { get; set; }
    public int ProfesionalId { get; set; }
    public int ObraSocialId { get; set; }
    public DateTime FechaHora { get; set; }
}
