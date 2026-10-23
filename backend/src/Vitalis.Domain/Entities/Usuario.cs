namespace Vitalis.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// True cuando la persona demostró que el correo es suyo (abrió el enlace
    /// de confirmación). Las cuentas del personal las crea el administrador y
    /// nacen confirmadas; las de pacientes que se registran solos desde la web
    /// nacen en false y no pueden iniciar sesión hasta confirmar.
    /// </summary>
    public bool EmailConfirmado { get; set; } = true;
    public DateTime? FechaConfirmacionEmail { get; set; }

    /// <summary>Contraseñas incorrectas seguidas. Se reinicia al entrar bien.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Mientras sea una fecha futura (UTC), la cuenta no puede iniciar sesión.</summary>
    public DateTime? BloqueadoHasta { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public Rol Rol { get; set; } = null!;
}
