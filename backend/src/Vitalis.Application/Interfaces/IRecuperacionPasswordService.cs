using Vitalis.Application.DTOs.Auth;

namespace Vitalis.Application.Interfaces;

/// <summary>
/// "Olvidé mi contraseña": pedir un enlace por correo y elegir una nueva.
/// Usa el mismo mecanismo de tokens de un solo uso que la confirmación del
/// registro (TokenVerificacion), con otro propósito y un vencimiento más corto.
/// </summary>
public interface IRecuperacionPasswordService
{
    /// <summary>
    /// Manda el enlace si la cuenta existe, está activa y confirmada. Devuelve
    /// SIEMPRE el mismo mensaje, para no revelar qué correos tienen cuenta.
    /// </summary>
    Task<MensajeDto> SolicitarAsync(string email, CancellationToken ct = default);

    /// <summary>Cambia la contraseña con el token del enlace. ValidationException si el token no sirve.</summary>
    Task<MensajeDto> RestablecerAsync(RestablecerPasswordDto dto, CancellationToken ct = default);
}
