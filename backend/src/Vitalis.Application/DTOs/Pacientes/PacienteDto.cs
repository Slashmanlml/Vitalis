using System.ComponentModel.DataAnnotations;

namespace Vitalis.Application.DTOs.Pacientes;

public class PacienteDto
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
    public string? ObraSocialNombre { get; set; }
    public string? NumeroAfiliado { get; set; }
    public string? FotoUrl { get; set; }
    public bool Activo { get; set; }
}

public class CrearPacienteDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El DNI es obligatorio")]
    // Solo números: antes se aceptaba cualquier texto de 6 a 20 caracteres
    // ("12ab456" pasaba si se llamaba a la API sin la pantalla). Mismo rango
    // que valida el formulario de recepción.
    [RegularExpression(@"^\d{6,9}$", ErrorMessage = "El DNI tiene que tener entre 6 y 9 números, sin puntos ni letras.")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
    public DateTime FechaNacimiento { get; set; }

    [EmailAddress(ErrorMessage = "El email no es válido")]
    public string? Email { get; set; }

    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public int? ObraSocialId { get; set; }
    public string? NumeroAfiliado { get; set; }
    public string? FotoUrl { get; set; }
}

public class EditarPacienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public int? ObraSocialId { get; set; }
    public string? NumeroAfiliado { get; set; }
    public string? FotoUrl { get; set; }
}
