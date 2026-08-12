using System.ComponentModel.DataAnnotations;
// CrearUsuarioDto.cs
using System.ComponentModel.DataAnnotations;

namespace Vitalis.Application.DTOs.Usuarios;

public class CrearUsuarioDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = string.Empty;
}
