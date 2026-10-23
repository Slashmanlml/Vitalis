using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Configuracion;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class RegistroPacienteServiceTests
{
    private const string Password = "Paciente123";

    private readonly VitalisDbContext _context;
    private readonly CapturadorEmailService _email = new();
    private readonly SeguridadOptions _opciones = new() { UrlFrontend = "http://localhost:4200", SegundosEntreReenvios = 60 };
    private readonly RegistroPacienteService _service;
    private readonly AuthService _auth;

    public RegistroPacienteServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new Microsoft.AspNetCore.Http.HttpContextAccessor());

        _context.Roles.AddRange(
            new Rol { Id = 1, Nombre = Roles.Administrador },
            new Rol { Id = 5, Nombre = Roles.Paciente });
        _context.SaveChanges();

        _service = new RegistroPacienteService(_context, _email, Options.Create(_opciones), NullLogger<RegistroPacienteService>.Instance);
        _auth = new AuthService(_context, new FakeJwtTokenService(), Options.Create(_opciones));
    }

    private static RegistroPacienteDto Dto(string email = "Juana.Perez@Example.com", string dni = "30111222") => new()
    {
        Nombre = "Juana",
        Apellido = "Pérez",
        Dni = dni,
        FechaNacimiento = new DateTime(1990, 3, 15),
        Email = email,
        Telefono = "3764-555555",
        Password = Password,
        ConfirmarPassword = Password,
        AceptaTerminos = true
    };

    [Fact]
    public async Task Registrar_crea_cuenta_sin_confirmar_ficha_inactiva_y_manda_el_enlace()
    {
        var r = await _service.RegistrarAsync(Dto());

        r.Mensaje.Should().Be(RegistroPacienteService.MensajeRegistro);

        var usuario = await _context.Usuarios.Include(u => u.Rol).SingleAsync(u => u.Rol.Nombre == Roles.Paciente);
        usuario.Email.Should().Be("juana.perez@example.com", "el correo se guarda normalizado");
        usuario.EmailConfirmado.Should().BeFalse();
        BCrypt.Net.BCrypt.Verify(Password, usuario.PasswordHash).Should().BeTrue();

        var paciente = await _context.Pacientes.SingleAsync();
        paciente.UsuarioId.Should().Be(usuario.Id);
        paciente.Activo.Should().BeFalse("la ficha se activa recién al confirmar");

        var token = _email.UltimoToken(EventoNotificacion.ConfirmacionEmail);
        token.Should().NotBeNullOrEmpty();
        _email.Ultimo(EventoNotificacion.ConfirmacionEmail)!.DatosSensibles.Should().Contain("UrlConfirmacion");

        var guardado = await _context.TokensVerificacion.SingleAsync();
        guardado.TokenHash.Should().Be(TokenSeguro.Hash(token!), "se guarda el hash");
        guardado.TokenHash.Should().NotBe(token, "nunca el token en claro");
    }

    [Fact]
    public async Task No_puede_iniciar_sesion_hasta_confirmar_el_correo()
    {
        await _service.RegistrarAsync(Dto());

        var intento = () => _auth.LoginAsync(new LoginRequestDto { Email = "juana.perez@example.com", Password = Password });

        var ex = await intento.Should().ThrowAsync<ForbiddenException>();
        ex.Which.Codigo.Should().Be(AuthService.CodigoEmailNoConfirmado);
    }

    [Fact]
    public async Task Confirmar_activa_cuenta_y_ficha_y_permite_entrar()
    {
        await _service.RegistrarAsync(Dto());
        var token = _email.UltimoToken(EventoNotificacion.ConfirmacionEmail)!;

        await _service.ConfirmarEmailAsync(token);

        var usuario = await _context.Usuarios.SingleAsync(u => u.Email == "juana.perez@example.com");
        usuario.EmailConfirmado.Should().BeTrue();
        usuario.FechaConfirmacionEmail.Should().NotBeNull();
        (await _context.Pacientes.SingleAsync()).Activo.Should().BeTrue();
        _email.Ultimo(EventoNotificacion.BienvenidaPaciente).Should().NotBeNull();

        var login = await _auth.LoginAsync(new LoginRequestDto { Email = "JUANA.PEREZ@example.com ", Password = Password });
        login.Should().NotBeNull();
        login!.Rol.Should().Be(Roles.Paciente);
    }

    [Fact]
    public async Task El_enlace_sirve_una_sola_vez()
    {
        await _service.RegistrarAsync(Dto());
        var token = _email.UltimoToken(EventoNotificacion.ConfirmacionEmail)!;
        await _service.ConfirmarEmailAsync(token);

        var otraVez = () => _service.ConfirmarEmailAsync(token);
        await otraVez.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Un_enlace_vencido_no_sirve()
    {
        await _service.RegistrarAsync(Dto());
        var token = _email.UltimoToken(EventoNotificacion.ConfirmacionEmail)!;
        var guardado = await _context.TokensVerificacion.SingleAsync();
        guardado.FechaExpiracion = DateTime.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();

        var confirmar = () => _service.ConfirmarEmailAsync(token);
        await confirmar.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("token-inventado-que-no-existe-en-la-base")]
    public async Task Un_token_inexistente_no_sirve(string token)
    {
        var confirmar = () => _service.ConfirmarEmailAsync(token);
        await confirmar.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Registrar_un_correo_que_ya_tiene_cuenta_responde_lo_mismo_y_no_crea_nada()
    {
        await _service.RegistrarAsync(Dto());
        await _service.ConfirmarEmailAsync(_email.UltimoToken(EventoNotificacion.ConfirmacionEmail)!);
        var correosAntes = _email.Pedidos.Count;

        var r = await _service.RegistrarAsync(Dto(dni: "40999888"));

        r.Mensaje.Should().Be(RegistroPacienteService.MensajeRegistro, "no se revela que el correo existe");
        (await _context.Usuarios.CountAsync()).Should().Be(1);
        (await _context.Pacientes.CountAsync()).Should().Be(1);
        _email.Pedidos.Count.Should().Be(correosAntes);
    }

    [Fact]
    public async Task Ficha_existente_con_el_mismo_correo_se_vincula_sin_duplicarla()
    {
        _context.Pacientes.Add(new Paciente
        {
            Nombre = "Juana", Apellido = "Pérez", Dni = "30111222", Email = "juana.perez@example.com",
            FechaNacimiento = new DateTime(1990, 3, 15), Activo = true
        });
        await _context.SaveChangesAsync();

        await _service.RegistrarAsync(Dto());

        var paciente = await _context.Pacientes.SingleAsync();
        paciente.UsuarioId.Should().NotBeNull();
        paciente.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task Ficha_existente_con_otro_correo_no_se_vincula()
    {
        // Saber el DNI de alguien no alcanza para quedarse con su ficha.
        _context.Pacientes.Add(new Paciente
        {
            Nombre = "Juana", Apellido = "Pérez", Dni = "30111222", Email = "otra@example.com",
            FechaNacimiento = new DateTime(1990, 3, 15), Activo = true
        });
        await _context.SaveChangesAsync();

        var registrar = () => _service.RegistrarAsync(Dto());

        await registrar.Should().ThrowAsync<ConflictException>();
        (await _context.Usuarios.CountAsync()).Should().Be(0);
        (await _context.Pacientes.SingleAsync()).UsuarioId.Should().BeNull();
    }

    [Fact]
    public async Task Ficha_dada_de_baja_no_se_reactiva_desde_la_web()
    {
        _context.Pacientes.Add(new Paciente
        {
            Nombre = "Juana", Apellido = "Pérez", Dni = "30111222", Email = "juana.perez@example.com",
            FechaNacimiento = new DateTime(1990, 3, 15), Activo = false
        });
        await _context.SaveChangesAsync();

        var registrar = () => _service.RegistrarAsync(Dto());

        await registrar.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Fecha_de_nacimiento_futura_se_rechaza()
    {
        var dto = Dto();
        dto.FechaNacimiento = DateTime.UtcNow.AddDays(3);

        var registrar = () => _service.RegistrarAsync(dto);
        await registrar.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Reenviar_respeta_la_espera_minima()
    {
        await _service.RegistrarAsync(Dto());
        var enviados = _email.Pedidos.Count(p => p.Evento == EventoNotificacion.ConfirmacionEmail);

        await _service.ReenviarConfirmacionAsync("juana.perez@example.com");

        _email.Pedidos.Count(p => p.Evento == EventoNotificacion.ConfirmacionEmail).Should().Be(enviados);
    }

    [Fact]
    public async Task Reenviar_invalida_el_enlace_anterior()
    {
        _opciones.SegundosEntreReenvios = 0;
        await _service.RegistrarAsync(Dto());
        var primero = _email.UltimoToken(EventoNotificacion.ConfirmacionEmail)!;

        await _service.ReenviarConfirmacionAsync("juana.perez@example.com");
        var segundo = _email.UltimoToken(EventoNotificacion.ConfirmacionEmail)!;

        segundo.Should().NotBe(primero);
        var usarViejo = () => _service.ConfirmarEmailAsync(primero);
        await usarViejo.Should().ThrowAsync<ValidationException>();
        await _service.ConfirmarEmailAsync(segundo);
    }

    [Fact]
    public async Task Reenviar_a_un_correo_inexistente_responde_lo_mismo_y_no_manda_nada()
    {
        var r = await _service.ReenviarConfirmacionAsync("nadie@example.com");

        r.Mensaje.Should().Be(RegistroPacienteService.MensajeReenvio);
        _email.Pedidos.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_registro_vencido_sin_confirmar_no_bloquea_el_correo_ni_el_dni()
    {
        await _service.RegistrarAsync(Dto());
        foreach (var t in _context.TokensVerificacion) t.FechaExpiracion = DateTime.UtcNow.AddMinutes(-5);
        await _context.SaveChangesAsync();

        await _service.RegistrarAsync(Dto());

        (await _context.Usuarios.CountAsync()).Should().Be(1, "el registro abandonado se reemplazó");
        (await _context.Pacientes.CountAsync()).Should().Be(1);
        var token = _email.UltimoToken(EventoNotificacion.ConfirmacionEmail)!;
        await _service.ConfirmarEmailAsync(token);
    }
}
