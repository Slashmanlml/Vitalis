using System.ComponentModel.DataAnnotations;
// EditarUsuarioDto.cs
using System.ComponentModel.DataAnnotations;

namespace Vitalis.Application.DTOs.Usuarios;

public class EditarUsuarioDto
{
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    public string? Rol { get; set; }
}
