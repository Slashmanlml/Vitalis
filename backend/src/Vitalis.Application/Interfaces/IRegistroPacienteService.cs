using Vitalis.Application.DTOs.Auth;

namespace Vitalis.Application.Interfaces;

/// <summary>
/// Alta de pacientes desde la web pública y confirmación del correo.
///
/// Flujo: Registrar → llega un mail con un enlace → ConfirmarEmail. Hasta
/// confirmar, la cuenta existe pero no puede iniciar sesión.
/// </summary>
public interface IRegistroPacienteService
{
    /// <summary>
    /// Crea la cuenta (sin confirmar) y manda el correo. Devuelve siempre el
    /// mismo mensaje genérico, exista o no el correo.
    /// Lanza ConflictException solo si el DNI ya pertenece a una ficha del
    /// consultorio con otro correo (hay que vincularla en recepción).
    /// </summary>
    Task<MensajeDto> RegistrarAsync(RegistroPacienteDto dto, CancellationToken ct = default);

    /// <summary>Valida el token del enlace y activa la cuenta. ValidationException si no sirve.</summary>
    Task<MensajeDto> ConfirmarEmailAsync(string token, CancellationToken ct = default);

    /// <summary>Manda un enlace nuevo si la cuenta existe y no está confirmada. Respuesta genérica.</summary>
    Task<MensajeDto> ReenviarConfirmacionAsync(string email, CancellationToken ct = default);
}
