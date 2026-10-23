namespace Vitalis.Domain.Entities;

/// <summary>
/// Token de un solo uso que se manda por correo (por ahora, para confirmar el
/// registro; el mismo mecanismo sirve para "olvidé mi contraseña").
///
/// En la base se guarda solo el HASH SHA-256 del token, nunca el token en
/// claro: si alguien lee la tabla (una copia de seguridad filtrada, un
/// administrador curioso) no puede usar lo que ve para confirmar cuentas
/// ajenas. Es el mismo criterio que con las contraseñas.
/// </summary>
public class TokenVerificacion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>SHA-256 del token, en hexadecimal (64 caracteres).</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Para qué sirve. Ver PropositoToken.</summary>
    public string Proposito { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }

    /// <summary>Cuándo se usó. Un token usado no vuelve a servir.</summary>
    public DateTime? FechaUso { get; set; }
}
