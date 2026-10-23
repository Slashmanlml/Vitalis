using System.ComponentModel.DataAnnotations;

namespace Vitalis.Application.DTOs.Auth;

public class ConfirmarEmailDto
{
    [Required, StringLength(200, MinimumLength = 20)]
    public string Token { get; set; } = string.Empty;
}

public class ReenviarConfirmacionDto
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta de registro y reenvío. Es SIEMPRE la misma, exista o no el
/// correo, para que el formulario no sirva para averiguar quién tiene cuenta
/// en el consultorio (enumeración de usuarios).
/// </summary>
public class MensajeDto
{
    public string Mensaje { get; set; } = string.Empty;
}
