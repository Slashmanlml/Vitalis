using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Configuracion;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

/// <summary>
/// Recuperación de contraseña por correo.
///
/// Decisiones (y por qué):
/// 1. Respuesta genérica al pedir el enlace: si dijera "ese correo no existe",
///    el formulario serviría para averiguar quién se atiende en el consultorio.
/// 2. El enlace vence en 1 hora (no 24 como el de confirmación): quien lo tiene
///    puede tomar la cuenta, así que su ventana de uso es corta.
/// 3. Un solo enlace vigente: pedir otro invalida el anterior, y usar uno
///    invalida cualquier otro pendiente.
/// 4. Al cambiarla se desbloquea la cuenta (si estaba bloqueada por intentos
///    fallidos) y se avisa por correo que la contraseña cambió: si no fue la
///    persona, se entera.
/// 5. Solo cuentas activas y con el correo confirmado: recuperar una cuenta sin
///    confirmar equivaldría a saltearse la confirmación.
/// </summary>
public class RecuperacionPasswordService : IRecuperacionPasswordService
{
    public const string MensajeSolicitud =
        "Si existe una cuenta con ese correo, te enviamos un enlace para elegir una contraseña nueva. Vence en 1 hora.";

    private readonly VitalisDbContext _context;
    private readonly IEmailService _emailService;
    private readonly SeguridadOptions _opciones;
    private readonly ILogger<RecuperacionPasswordService> _logger;

    public RecuperacionPasswordService(
        VitalisDbContext context,
        IEmailService emailService,
        IOptions<SeguridadOptions> opciones,
        ILogger<RecuperacionPasswordService> logger)
    {
        _context = context;
        _emailService = emailService;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<MensajeDto> SolicitarAsync(string email, CancellationToken ct = default)
    {
        var normalizado = TokenSeguro.NormalizarEmail(email);
        var ahora = DateTime.UtcNow;

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == normalizado, ct);
        if (usuario is null || !usuario.Activo || !usuario.EmailConfirmado)
        {
            _logger.LogInformation("Pedido de recuperación para un correo sin cuenta utilizable.");
            return new MensajeDto { Mensaje = MensajeSolicitud };
        }

        var ultimo = await _context.TokensVerificacion
            .Where(t => t.UsuarioId == usuario.Id && t.Proposito == PropositoToken.RecuperarPassword)
            .OrderByDescending(t => t.FechaCreacion)
            .Select(t => (DateTime?)t.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        if (ultimo is not null && (ahora - ultimo.Value).TotalSeconds < _opciones.SegundosEntreReenvios)
        {
            _logger.LogInformation("Pedido de recuperación ignorado por espera mínima. UsuarioId {UsuarioId}.", usuario.Id);
            return new MensajeDto { Mensaje = MensajeSolicitud };
        }

        await InvalidarPendientesAsync(usuario.Id, ahora, ct);

        var token = TokenSeguro.Generar();
        _context.TokensVerificacion.Add(new TokenVerificacion
        {
            UsuarioId = usuario.Id,
            TokenHash = TokenSeguro.Hash(token),
            Proposito = PropositoToken.RecuperarPassword,
            FechaCreacion = ahora,
            FechaExpiracion = ahora.AddMinutes(_opciones.MinutosValidezRecuperacion)
        });
        await _context.SaveChangesAsync(ct);

        var url = $"{_opciones.UrlFrontend.TrimEnd('/')}/restablecer-password?token={Uri.EscapeDataString(token)}";

        await _emailService.NotificarAsync(new NotificacionRequest
        {
            Destinatario = usuario.Email,
            Evento = EventoNotificacion.RecuperarPassword,
            Datos = new Dictionary<string, string>
            {
                ["PacienteNombre"] = $"{usuario.Nombre} {usuario.Apellido}",
                ["UrlRecuperacion"] = url,
                ["MinutosValidez"] = _opciones.MinutosValidezRecuperacion.ToString()
            },
            DatosSensibles = ["UrlRecuperacion"]
        });

        _logger.LogInformation("Enlace de recuperación enviado. UsuarioId {UsuarioId}.", usuario.Id);
        return new MensajeDto { Mensaje = MensajeSolicitud };
    }

    public async Task<MensajeDto> RestablecerAsync(RestablecerPasswordDto dto, CancellationToken ct = default)
    {
        const string invalido = "El enlace no es válido o ya venció. Pedí uno nuevo desde \"Olvidé mi contraseña\".";
        var ahora = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            throw new ValidationException(invalido);
        }

        var hash = TokenSeguro.Hash(dto.Token.Trim());
        var registro = await _context.TokensVerificacion
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.Proposito == PropositoToken.RecuperarPassword, ct);

        if (registro?.Usuario is null || registro.FechaUso is not null || registro.FechaExpiracion <= ahora || !registro.Usuario.Activo)
        {
            _logger.LogWarning("Intento de restablecer contraseña con un token inválido, usado o vencido.");
            throw new ValidationException(invalido);
        }

        var usuario = registro.Usuario;
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNuevo);
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        registro.FechaUso = ahora;
        await InvalidarPendientesAsync(usuario.Id, ahora, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Contraseña restablecida por correo. UsuarioId {UsuarioId}.", usuario.Id);

        await _emailService.NotificarAsync(new NotificacionRequest
        {
            Destinatario = usuario.Email,
            Evento = EventoNotificacion.PasswordCambiada,
            Datos = new Dictionary<string, string>
            {
                ["PacienteNombre"] = $"{usuario.Nombre} {usuario.Apellido}"
            }
        });

        return new MensajeDto { Mensaje = "Listo, tu contraseña quedó cambiada. Ya podés iniciar sesión con la nueva." };
    }

    private async Task InvalidarPendientesAsync(int usuarioId, DateTime ahora, CancellationToken ct)
    {
        var pendientes = await _context.TokensVerificacion
            .Where(t => t.UsuarioId == usuarioId && t.Proposito == PropositoToken.RecuperarPassword && t.FechaUso == null)
            .ToListAsync(ct);
        foreach (var t in pendientes)
        {
            t.FechaUso = ahora;
        }
    }
}
