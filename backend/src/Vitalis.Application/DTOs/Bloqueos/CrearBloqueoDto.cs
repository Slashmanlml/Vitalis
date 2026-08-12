using System;

namespace Vitalis.Application.DTOs.Bloqueos;

public class CrearBloqueoDto
{
    public int ProfesionalId { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
