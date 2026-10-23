namespace Vitalis.Domain.Entities;

public class Paciente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public int? ObraSocialId { get; set; }
    public ObraSocial? ObraSocial { get; set; }
    public string? NumeroAfiliado { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cuenta del portal de pacientes vinculada a esta ficha, si el paciente se
    /// registró desde la web. Es el mismo patrón que Profesional.UsuarioId: la
    /// identidad sale del token y se traduce a "qué ficha es la mía" en el
    /// servidor, nunca a partir de un id que mande el navegador.
    /// </summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
