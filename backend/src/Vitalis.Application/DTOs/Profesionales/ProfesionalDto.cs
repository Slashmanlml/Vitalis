namespace Vitalis.Application.DTOs.Profesionales;

public class ProfesionalDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public string EspecialidadNombre { get; set; } = string.Empty;
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; }
}
