namespace Vitalis.Application.Interfaces;

/// <summary>
/// Responde "quien esta haciendo este pedido" a partir del token, no del cuerpo
/// del pedido.
///
/// Por que existe: hasta esta version los servicios confiaban en el
/// ProfesionalId que mandaba el navegador. Eso permitia que un medico
/// registrara una consulta sobre el turno de otro simplemente enviando otro id.
/// La identidad del que opera tiene que salir del token firmado, nunca del
/// cuerpo del pedido, que el cliente controla por completo.
/// </summary>
public interface IUsuarioActual
{
    /// <summary>Id del usuario autenticado, o null si no hay sesion.</summary>
    int? UsuarioId { get; }

    /// <summary>Nombre del rol tal como viaja en el token.</summary>
    string? Rol { get; }

    /// <summary>True solo si el rol del token es Medico.</summary>
    bool EsMedico { get; }

    /// <summary>
    /// Id del profesional vinculado al usuario autenticado (via
    /// Profesional.UsuarioId), o null si la cuenta no esta vinculada a ninguno.
    /// </summary>
    Task<int?> ObtenerProfesionalIdAsync(CancellationToken ct = default);
}
