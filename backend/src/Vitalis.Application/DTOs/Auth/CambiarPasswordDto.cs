using System.ComponentModel.DataAnnotations;
using Vitalis.Application.Validation;
namespace Vitalis.Application.DTOs.Auth;

public class CambiarPasswordDto
{
    [Required]
    public string PasswordActual { get; set; } = string.Empty;
    [Required, PasswordSegura]
    public string PasswordNuevo { get; set; } = string.Empty;
}
