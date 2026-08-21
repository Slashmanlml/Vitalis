namespace Vitalis.Application.DTOs.Profesionales;

public class EditarProfesionalDto
{
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Matricula { get; set; }
    public string? Email { get; set; }
    public int? EspecialidadId { get; set; }
    public string? FotoUrl { get; set; }
    public bool? Activo { get; set; }
}
