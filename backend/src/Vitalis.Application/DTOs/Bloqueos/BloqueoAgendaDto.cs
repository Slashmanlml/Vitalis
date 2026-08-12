namespace Vitalis.Application.DTOs.Bloqueos;

public class BloqueoAgendaDto
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
