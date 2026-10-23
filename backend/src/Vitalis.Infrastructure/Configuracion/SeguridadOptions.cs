namespace Vitalis.Infrastructure.Configuracion;

/// <summary>
/// Parámetros de seguridad de la cuenta. Sección "Seguridad" del appsettings.
/// Los valores por defecto son los recomendados; se pueden ajustar sin tocar código.
/// </summary>
public class SeguridadOptions
{
    public const string SectionName = "Seguridad";

    /// <summary>Contraseñas incorrectas seguidas antes de bloquear la cuenta.</summary>
    public int MaxIntentosFallidos { get; set; } = 5;

    /// <summary>Duración del bloqueo temporal.</summary>
    public int MinutosBloqueo { get; set; } = 15;

    /// <summary>Vigencia del enlace de confirmación de correo.</summary>
    public int HorasValidezConfirmacion { get; set; } = 24;

    /// <summary>Tiempo mínimo entre dos reenvíos del correo de confirmación.</summary>
    public int SegundosEntreReenvios { get; set; } = 60;

    /// <summary>
    /// Dirección pública del frontend, para armar el enlace del correo
    /// (ej.: https://vitalis.midominio.com). En desarrollo, el de ng serve.
    /// </summary>
    public string UrlFrontend { get; set; } = "http://localhost:4200";
}
