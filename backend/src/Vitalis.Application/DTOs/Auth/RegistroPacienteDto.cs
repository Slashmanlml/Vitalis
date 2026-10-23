using System.ComponentModel.DataAnnotations;
using Vitalis.Application.Validation;

namespace Vitalis.Application.DTOs.Auth;

/// <summary>Lo que completa un paciente en la página pública "Registrarme".</summary>
public class RegistroPacienteDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Solo dígitos, 7 u 8 (DNI argentino).</summary>
    [Required, RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos, sin puntos.")]
    public string Dni { get; set; } = string.Empty;

    [Required]
    public DateTime FechaNacimiento { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Telefono { get; set; }

    [Required, PasswordSegura]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    /// <summary>Aceptación de términos y del tratamiento de datos de salud (Ley 25.326).</summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "Tenés que aceptar los términos y la política de privacidad.")]
    public bool AceptaTerminos { get; set; }
}
