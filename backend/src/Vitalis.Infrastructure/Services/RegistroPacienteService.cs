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
/// Registro de pacientes desde la web y confirmación del correo.
///
/// Decisiones de diseño (y por qué):
///
/// 1. La cuenta nace SIN confirmar y no puede iniciar sesión. Confirmar el
///    correo prueba que quien se registra controla esa casilla; sin eso,
///    cualquiera podría crear cuentas a nombre de otro.
///
/// 2. Respuesta genérica. Registrar y reenviar devuelven el mismo mensaje
///    exista o no el correo. Si el formulario dijera "ese correo ya está
///    registrado", serviría para averiguar quién se atiende en el consultorio.
///
/// 3. Ficha existente. Si el DNI ya tiene una ficha cargada por recepción:
///      - con el MISMO correo → se vincula (al confirmar ese correo, la persona
///        prueba que es la dueña de la ficha);
///      - con otro correo o sin correo → NO se vincula automáticamente. De lo
///        contrario, sabiendo el DNI de alguien se podría tomar su ficha y ver
///        sus turnos. Se pide pasar por recepción.
///
/// 4. Ficha nueva. Si el DNI no existe, se crea la ficha inactiva y se activa
///    recién al confirmar, para que los registros abandonados no aparezcan
///    como pacientes activos del consultorio.
///
/// 5. Registros vencidos. Si alguien se registró y nunca confirmó, ese correo
///    y ese DNI no quedan tomados para siempre: pasado el vencimiento del
///    enlace, un registro nuevo reemplaza al abandonado.
/// </summary>
public class RegistroPacienteService : IRegistroPacienteService
{
    public const string MensajeRegistro =
        "Si los datos son correctos, te enviamos un correo con un enlace para confirmar tu cuenta. Revisá también la carpeta de spam.";

    public const string MensajeReenvio =
        "Si existe una cuenta pendiente de confirmación con ese correo, te enviamos un enlace nuevo.";

    private readonly VitalisDbContext _context;
    private readonly IEmailService _emailService;
    private readonly SeguridadOptions _opciones;
    private readonly ILogger<RegistroPacienteService> _logger;

    public RegistroPacienteService(
        VitalisDbContext context,
        IEmailService emailService,
        IOptions<SeguridadOptions> opciones,
        ILogger<RegistroPacienteService> logger)
    {
        _context = context;
        _emailService = emailService;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<MensajeDto> RegistrarAsync(RegistroPacienteDto dto, CancellationToken ct = default)
    {
        var email = TokenSeguro.NormalizarEmail(dto.Email);
        var dni = dto.Dni.Trim();
        var ahora = DateTime.UtcNow;

        ValidarFechaNacimiento(dto.FechaNacimiento, ahora);

        // --- ¿Ya hay una cuenta con este correo? ---
        var usuarioExistente = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (usuarioExistente is not null)
        {
            if (usuarioExistente.EmailConfirmado || !await EsRegistroVencidoAsync(usuarioExistente.Id, ahora, ct))
            {
                // Cuenta ya activa, o registro pendiente todavía vigente: no se
                // crea nada y se responde lo mismo que en el caso feliz. Si está
                // pendiente, se reenvía el enlace (respetando la espera mínima).
                if (!usuarioExistente.EmailConfirmado)
                {
                    await EnviarConfirmacionSiCorrespondeAsync(usuarioExistente, ahora, ct);
                }

                _logger.LogInformation("Registro con correo ya existente. UsuarioId {UsuarioId}.", usuarioExistente.Id);
                return new MensajeDto { Mensaje = MensajeRegistro };
            }

            // Registro abandonado y vencido: se libera para empezar de nuevo.
            await LiberarRegistroVencidoAsync(usuarioExistente, ct);
        }

        // --- ¿Ya hay una ficha con este DNI? ---
        var paciente = await _context.Pacientes
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Dni == dni, ct);

        if (paciente?.Usuario is { } duenio)
        {
            if (duenio.EmailConfirmado || !await EsRegistroVencidoAsync(duenio.Id, ahora, ct))
            {
                // La ficha ya tiene una cuenta. No se revela; quien sea el
                // dueño ya recibe sus correos.
                _logger.LogWarning("Intento de registro con un DNI que ya tiene cuenta. PacienteId {PacienteId}.", paciente.Id);
                return new MensajeDto { Mensaje = MensajeRegistro };
            }

            await LiberarRegistroVencidoAsync(duenio, ct);
            paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Dni == dni, ct);
        }

        if (paciente is not null && !paciente.Activo)
        {
            // Ficha dada de baja por el consultorio: no se reactiva desde la web.
            _logger.LogWarning("Registro rechazado: el DNI pertenece a una ficha dada de baja. PacienteId {PacienteId}.", paciente.Id);
            throw new ConflictException(
                "Ya existe una ficha en el consultorio con ese DNI. Para proteger tus datos, acercate a recepción " +
                "(o llamá) para que vinculen tu correo a la ficha, y después volvé a registrarte.");
        }

        if (paciente is not null)
        {
            var emailFicha = paciente.Email is null ? null : TokenSeguro.NormalizarEmail(paciente.Email);
            if (emailFicha != email)
            {
                // Ver punto 3 del comentario de la clase.
                _logger.LogWarning("Registro rechazado: el DNI pertenece a una ficha con otro correo. PacienteId {PacienteId}.", paciente.Id);
                throw new ConflictException(
                    "Ya existe una ficha en el consultorio con ese DNI. Para proteger tus datos, acercate a recepción " +
                    "(o llamá) para que vinculen tu correo a la ficha, y después volvé a registrarte.");
            }
        }

        var rolPaciente = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == Roles.Paciente, ct)
            ?? throw new InvalidOperationException("No existe el rol Paciente en la base de datos.");

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RolId = rolPaciente.Id,
            Activo = true,
            EmailConfirmado = false,
            FechaCreacion = ahora
        };
        _context.Usuarios.Add(usuario);

        if (paciente is null)
        {
            paciente = new Paciente
            {
                Nombre = dto.Nombre.Trim(),
                Apellido = dto.Apellido.Trim(),
                Dni = dni,
                FechaNacimiento = DateTime.SpecifyKind(dto.FechaNacimiento.Date, DateTimeKind.Utc),
                Email = email,
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                Activo = false, // se activa al confirmar (punto 4)
                FechaCreacion = ahora
            };
            _context.Pacientes.Add(paciente);
        }

        paciente.Usuario = usuario;
        await _context.SaveChangesAsync(ct);

        await EnviarConfirmacionAsync(usuario, ahora, ct);

        _logger.LogInformation("Paciente registrado desde la web, pendiente de confirmación. UsuarioId {UsuarioId}.", usuario.Id);
        return new MensajeDto { Mensaje = MensajeRegistro };
    }

    public async Task<MensajeDto> ConfirmarEmailAsync(string token, CancellationToken ct = default)
    {
        const string invalido =
            "El enlace no es válido o ya venció. Pedí uno nuevo desde la pantalla de inicio de sesión.";

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ValidationException(invalido);
        }

        var hash = TokenSeguro.Hash(token.Trim());
        var ahora = DateTime.UtcNow;

        var registro = await _context.TokensVerificacion
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.Proposito == PropositoToken.ConfirmacionEmail, ct);

        if (registro?.Usuario is null || registro.FechaUso is not null || registro.FechaExpiracion <= ahora)
        {
            _logger.LogWarning("Intento de confirmación con un token inválido, usado o vencido.");
            throw new ValidationException(invalido);
        }

        var usuario = registro.Usuario;
        registro.FechaUso = ahora;

        if (!usuario.EmailConfirmado)
        {
            usuario.EmailConfirmado = true;
            usuario.FechaConfirmacionEmail = ahora;
        }

        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, ct);
        if (paciente is not null && !paciente.Activo)
        {
            paciente.Activo = true;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Correo confirmado. UsuarioId {UsuarioId}.", usuario.Id);

        await _emailService.NotificarAsync(new NotificacionRequest
        {
            Destinatario = usuario.Email,
            Evento = EventoNotificacion.BienvenidaPaciente,
            Datos = new Dictionary<string, string>
            {
                ["PacienteNombre"] = $"{usuario.Nombre} {usuario.Apellido}"
            }
        });

        return new MensajeDto { Mensaje = "¡Listo! Tu correo quedó confirmado. Ya podés iniciar sesión." };
    }

    public async Task<MensajeDto> ReenviarConfirmacionAsync(string email, CancellationToken ct = default)
    {
        var normalizado = TokenSeguro.NormalizarEmail(email);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == normalizado, ct);

        if (usuario is not null && !usuario.EmailConfirmado && usuario.Activo)
        {
            await EnviarConfirmacionSiCorrespondeAsync(usuario, DateTime.UtcNow, ct);
        }

        return new MensajeDto { Mensaje = MensajeReenvio };
    }

    // ------------------------------------------------------------------

    private static void ValidarFechaNacimiento(DateTime fecha, DateTime ahora)
    {
        if (fecha.Date > ahora.Date || fecha.Year < 1900)
        {
            throw new ValidationException("La fecha de nacimiento no es válida.");
        }
    }

    /// <summary>
    /// Manda un enlace nuevo salvo que se haya mandado otro hace muy poco
    /// (evita usar el formulario para inundar la casilla de alguien).
    /// En ese caso no hace nada: la respuesta al usuario es la misma.
    /// </summary>
    private async Task EnviarConfirmacionSiCorrespondeAsync(Usuario usuario, DateTime ahora, CancellationToken ct)
    {
        var ultimo = await _context.TokensVerificacion
            .Where(t => t.UsuarioId == usuario.Id && t.Proposito == PropositoToken.ConfirmacionEmail)
            .OrderByDescending(t => t.FechaCreacion)
            .Select(t => (DateTime?)t.FechaCreacion)
            .FirstOrDefaultAsync(ct);

        if (ultimo is not null && (ahora - ultimo.Value).TotalSeconds < _opciones.SegundosEntreReenvios)
        {
            _logger.LogInformation("Reenvío de confirmación ignorado por espera mínima. UsuarioId {UsuarioId}.", usuario.Id);
            return;
        }

        await EnviarConfirmacionAsync(usuario, ahora, ct);
    }

    private async Task EnviarConfirmacionAsync(Usuario usuario, DateTime ahora, CancellationToken ct)
    {
        // Un solo enlace vigente por cuenta: los anteriores dejan de servir.
        var anteriores = await _context.TokensVerificacion
            .Where(t => t.UsuarioId == usuario.Id && t.Proposito == PropositoToken.ConfirmacionEmail && t.FechaUso == null)
            .ToListAsync(ct);
        foreach (var t in anteriores)
        {
            t.FechaUso = ahora;
        }

        var token = TokenSeguro.Generar();
        _context.TokensVerificacion.Add(new TokenVerificacion
        {
            UsuarioId = usuario.Id,
            TokenHash = TokenSeguro.Hash(token),
            Proposito = PropositoToken.ConfirmacionEmail,
            FechaCreacion = ahora,
            FechaExpiracion = ahora.AddHours(_opciones.HorasValidezConfirmacion)
        });
        await _context.SaveChangesAsync(ct);

        var url = $"{_opciones.UrlFrontend.TrimEnd('/')}/confirmar-email?token={Uri.EscapeDataString(token)}";

        await _emailService.NotificarAsync(new NotificacionRequest
        {
            Destinatario = usuario.Email,
            Evento = EventoNotificacion.ConfirmacionEmail,
            Datos = new Dictionary<string, string>
            {
                ["PacienteNombre"] = $"{usuario.Nombre} {usuario.Apellido}",
                ["UrlConfirmacion"] = url,
                ["HorasValidez"] = _opciones.HorasValidezConfirmacion.ToString()
            },
            DatosSensibles = ["UrlConfirmacion"]
        });
    }

    /// <summary>True si la cuenta nunca se confirmó y todos sus enlaces vencieron.</summary>
    private async Task<bool> EsRegistroVencidoAsync(int usuarioId, DateTime ahora, CancellationToken ct)
    {
        var hayVigente = await _context.TokensVerificacion.AnyAsync(t =>
            t.UsuarioId == usuarioId &&
            t.Proposito == PropositoToken.ConfirmacionEmail &&
            t.FechaUso == null &&
            t.FechaExpiracion > ahora, ct);

        return !hayVigente;
    }

    /// <summary>
    /// Borra una cuenta que nunca se confirmó. Si la ficha la creó ese mismo
    /// registro (sigue inactiva y sin turnos), se borra también; si era una
    /// ficha que ya existía en el consultorio, solo se desvincula.
    /// </summary>
    private async Task LiberarRegistroVencidoAsync(Usuario usuario, CancellationToken ct)
    {
        if (usuario.EmailConfirmado)
        {
            throw new InvalidOperationException("No se puede liberar una cuenta confirmada.");
        }

        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, ct);
        if (paciente is not null)
        {
            var tieneTurnos = await _context.Turnos.AnyAsync(t => t.PacienteId == paciente.Id, ct);
            if (!paciente.Activo && !tieneTurnos)
            {
                _context.Pacientes.Remove(paciente);
            }
            else
            {
                paciente.UsuarioId = null;
            }
        }

        _context.Usuarios.Remove(usuario); // los tokens se borran en cascada
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Registro vencido sin confirmar liberado. UsuarioId {UsuarioId}.", usuario.Id);
    }
}
