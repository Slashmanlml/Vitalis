namespace Vitalis.Application.DTOs.Profesionales;

public class CrearProfesionalDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public string? FotoUrl { get; set; }
}
