using System.ComponentModel.DataAnnotations;
using Vitalis.Application.Validation;

namespace Vitalis.Application.DTOs.Auth;

/// <summary>Pedido de "olvidé mi contraseña": solo el correo.</summary>
public class SolicitarRecuperacionDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Contraseña nueva elegida desde el enlace del correo.</summary>
public class RestablecerPasswordDto
{
    [Required, StringLength(200, MinimumLength = 20)]
    public string Token { get; set; } = string.Empty;

    [Required, PasswordSegura]
    public string PasswordNuevo { get; set; } = string.Empty;

    [Required, Compare(nameof(PasswordNuevo), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}
