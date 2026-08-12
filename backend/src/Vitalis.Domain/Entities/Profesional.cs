namespace Vitalis.Domain.Entities;

public class Profesional
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public Especialidad? Especialidad { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
