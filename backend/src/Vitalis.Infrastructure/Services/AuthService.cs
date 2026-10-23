using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Configuracion;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class AuthService : IAuthService
{
    public const string CodigoEmailNoConfirmado = "EMAIL_NO_CONFIRMADO";
    public const string CodigoCuentaBloqueada = "CUENTA_BLOQUEADA";

    /// <summary>
    /// Hash bcrypt de una contraseña cualquiera. Cuando el correo no existe se
    /// verifica contra este hash igual, para que la respuesta tarde lo mismo
    /// que con un correo real: si no, midiendo el tiempo de respuesta se podría
    /// saber qué correos tienen cuenta (enumeración por tiempo).
    /// </summary>
    private static readonly string HashDeRelleno = BCrypt.Net.BCrypt.HashPassword("relleno-para-igualar-tiempos");

    private readonly VitalisDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly SeguridadOptions _opciones;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        VitalisDbContext context,
        IJwtTokenService jwtTokenService,
        IOptions<SeguridadOptions>? opciones = null,
        ILogger<AuthService>? logger = null)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _opciones = opciones?.Value ?? new SeguridadOptions();
        _logger = logger ?? NullLogger<AuthService>.Instance;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = TokenSeguro.NormalizarEmail(request.Email);
        var ahora = DateTime.UtcNow;

        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo, cancellationToken);

        if (usuario is null)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, HashDeRelleno);
            _logger.LogWarning("Login fallido: correo inexistente o cuenta inactiva.");
            return null;
        }

        // Bloqueo temporal por fuerza bruta. Se chequea ANTES de mirar la
        // contraseña: durante el bloqueo ni siquiera la correcta entra, así un
        // atacante no puede seguir probando.
        if (usuario.BloqueadoHasta is { } hasta && hasta > ahora)
        {
            _logger.LogWarning("Login rechazado: cuenta bloqueada. UsuarioId {UsuarioId}.", usuario.Id);
            throw new DemasiadosIntentosException(MensajeBloqueo(hasta, ahora), CodigoCuentaBloqueada);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= _opciones.MaxIntentosFallidos)
            {
                usuario.BloqueadoHasta = ahora.AddMinutes(_opciones.MinutosBloqueo);
                usuario.IntentosFallidos = 0;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Cuenta bloqueada por {Max} intentos fallidos. UsuarioId {UsuarioId}.",
                    _opciones.MaxIntentosFallidos, usuario.Id);
                throw new DemasiadosIntentosException(MensajeBloqueo(usuario.BloqueadoHasta.Value, ahora), CodigoCuentaBloqueada);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Login fallido: contraseña incorrecta. UsuarioId {UsuarioId}, intento {Intento}.",
                usuario.Id, usuario.IntentosFallidos);
            return null;
        }

        // Contraseña correcta pero correo sin confirmar: recién acá se dice,
        // porque quien llega hasta este punto ya demostró conocer la contraseña.
        if (!usuario.EmailConfirmado)
        {
            _logger.LogInformation("Login rechazado: correo sin confirmar. UsuarioId {UsuarioId}.", usuario.Id);
            throw new ForbiddenException(
                "Todavía no confirmaste tu correo. Revisá tu casilla (y la carpeta de spam) o pedí un enlace nuevo.",
                CodigoEmailNoConfirmado);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = ahora;
        await _context.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = _jwtTokenService.GenerateToken(usuario);

        _logger.LogInformation("Login correcto. UsuarioId {UsuarioId}, rol {Rol}.", usuario.Id, usuario.Rol.Nombre);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UsuarioId = usuario.Id,
            Email = usuario.Email,
            NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}",
            Rol = usuario.Rol.Nombre
        };
    }

    public async Task<UsuarioPerfilDto?> ObtenerPerfilAsync(int usuarioId)
    {
        return await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.Id == usuarioId)
            .Select(u => new UsuarioPerfilDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Rol = u.Rol.Nombre
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
        {
            _logger.LogWarning("Cambio de contraseña rechazado: la actual no coincide. UsuarioId {UsuarioId}.", usuarioId);
            return false;
        }

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNuevo);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Contraseña cambiada. UsuarioId {UsuarioId}.", usuarioId);
        return true;
    }

    private static string MensajeBloqueo(DateTime hasta, DateTime ahora)
    {
        var minutos = Math.Max(1, (int)Math.Ceiling((hasta - ahora).TotalMinutes));
        return $"La cuenta quedó bloqueada temporalmente por demasiados intentos fallidos. Probá de nuevo en {minutos} minuto(s).";
    }
}
