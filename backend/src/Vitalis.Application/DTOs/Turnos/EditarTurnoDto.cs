namespace Vitalis.Application.DTOs.Turnos;

public class EditarTurnoDto
{
    public int PacienteId { get; set; }
    public int ProfesionalId { get; set; }
    public int ObraSocialId { get; set; }
    public DateTime FechaHora { get; set; }
    public bool Confirmado { get; set; }
    public string? Estado { get; set; }
}
