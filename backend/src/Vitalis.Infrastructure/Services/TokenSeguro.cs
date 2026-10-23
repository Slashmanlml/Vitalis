using System.Security.Cryptography;
using System.Text;

namespace Vitalis.Infrastructure.Services;

/// <summary>
/// Genera tokens para enlaces de correo y calcula su hash.
///
/// - 32 bytes de RandomNumberGenerator (generador criptográfico del sistema
///   operativo): 256 bits, imposible de adivinar. Random/Guid no sirven para
///   esto: no están pensados para ser impredecibles.
/// - Se codifica en Base64Url para que viaje en una URL sin escapar nada.
/// - En la base solo se guarda el SHA-256. Alcanza un hash rápido (y no
///   bcrypt como con las contraseñas) porque el token ya es aleatorio y largo:
///   no hay diccionario posible contra 256 bits.
/// </summary>
public static class TokenSeguro
{
    public static string Generar()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Normaliza un correo para guardarlo y compararlo siempre igual.</summary>
    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
