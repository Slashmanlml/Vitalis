using System;
using System.Collections.Generic;
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

public class RecuperacionPasswordServiceTests
{
    private const string Email = "juana@example.com";
    private const string PasswordVieja = "Vieja12345";
    private const string PasswordNueva = "Nueva12345!";

    private readonly VitalisDbContext _context;
    private readonly CapturadorEmailService _email = new();
    private readonly SeguridadOptions _opciones = new() { UrlFrontend = "http://localhost:4200", SegundosEntreReenvios = 60 };
    private readonly RecuperacionPasswordService _service;
    private readonly AuthService _auth;

    public RecuperacionPasswordServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new Microsoft.AspNetCore.Http.HttpContextAccessor());
        _context.Roles.Add(new Rol { Id = 5, Nombre = Roles.Paciente });
        _context.Usuarios.Add(new Usuario
        {
            Id = 1, Nombre = "Juana", Apellido = "Pérez", Email = Email, RolId = 5, Activo = true,
            EmailConfirmado = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordVieja), FechaCreacion = DateTime.UtcNow
        });
        _context.SaveChanges();

        _service = new RecuperacionPasswordService(_context, _email, Options.Create(_opciones), NullLogger<RecuperacionPasswordService>.Instance);
        _auth = new AuthService(_context, new FakeJwtTokenService(), Options.Create(_opciones));
    }

    private string? TokenDelCorreo()
    {
        var url = _email.Ultimo(EventoNotificacion.RecuperarPassword)?.Datos.GetValueOrDefault("UrlRecuperacion");
        if (url is null) return null;
        return Uri.UnescapeDataString(url[(url.IndexOf("token=", StringComparison.Ordinal) + 6)..]);
    }

    private static RestablecerPasswordDto Dto(string token, string nueva = PasswordNueva) =>
        new() { Token = token, PasswordNuevo = nueva, ConfirmarPassword = nueva };

    [Fact]
    public async Task Pedir_el_enlace_manda_un_correo_con_token_de_1_hora()
    {
        var r = await _service.SolicitarAsync("  JUANA@example.com ");

        r.Mensaje.Should().Be(RecuperacionPasswordService.MensajeSolicitud);
        TokenDelCorreo().Should().NotBeNullOrEmpty();
        _email.Ultimo(EventoNotificacion.RecuperarPassword)!.DatosSensibles.Should().Contain("UrlRecuperacion");

        var guardado = await _context.TokensVerificacion.SingleAsync();
        guardado.Proposito.Should().Be(PropositoToken.RecuperarPassword);
        guardado.TokenHash.Should().Be(TokenSeguro.Hash(TokenDelCorreo()!));
        (guardado.FechaExpiracion - guardado.FechaCreacion).TotalMinutes.Should().BeApproximately(60, 0.1);
    }

    [Fact]
    public async Task Correo_inexistente_responde_lo_mismo_y_no_manda_nada()
    {
        var r = await _service.SolicitarAsync("nadie@example.com");

        r.Mensaje.Should().Be(RecuperacionPasswordService.MensajeSolicitud);
        _email.Pedidos.Should().BeEmpty();
    }

    [Fact]
    public async Task Cuenta_sin_confirmar_no_recibe_enlace()
    {
        var u = await _context.Usuarios.SingleAsync();
        u.EmailConfirmado = false;
        await _context.SaveChangesAsync();

        await _service.SolicitarAsync(Email);

        _email.Pedidos.Should().BeEmpty();
    }

    [Fact]
    public async Task Restablecer_cambia_la_contraseña_avisa_y_desbloquea()
    {
        var u = await _context.Usuarios.SingleAsync();
        u.BloqueadoHasta = DateTime.UtcNow.AddMinutes(10);
        u.IntentosFallidos = 3;
        await _context.SaveChangesAsync();

        await _service.SolicitarAsync(Email);
        await _service.RestablecerAsync(Dto(TokenDelCorreo()!));

        var login = await _auth.LoginAsync(new LoginRequestDto { Email = Email, Password = PasswordNueva });
        login.Should().NotBeNull("la nueva funciona y la cuenta quedó desbloqueada");
        (await _auth.LoginAsync(new LoginRequestDto { Email = Email, Password = PasswordVieja })).Should().BeNull();
        _email.Ultimo(EventoNotificacion.PasswordCambiada).Should().NotBeNull();
    }

    [Fact]
    public async Task El_enlace_sirve_una_sola_vez()
    {
        await _service.SolicitarAsync(Email);
        var token = TokenDelCorreo()!;
        await _service.RestablecerAsync(Dto(token));

        var otraVez = () => _service.RestablecerAsync(Dto(token, "Otra12345!"));
        await otraVez.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Un_enlace_vencido_no_sirve()
    {
        await _service.SolicitarAsync(Email);
        var t = await _context.TokensVerificacion.SingleAsync();
        t.FechaExpiracion = DateTime.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();

        var usar = () => _service.RestablecerAsync(Dto(TokenDelCorreo()!));
        await usar.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Pedir_otro_invalida_el_anterior()
    {
        _opciones.SegundosEntreReenvios = 0;
        await _service.SolicitarAsync(Email);
        var primero = TokenDelCorreo()!;
        await _service.SolicitarAsync(Email);
        var segundo = TokenDelCorreo()!;

        segundo.Should().NotBe(primero);
        var usarViejo = () => _service.RestablecerAsync(Dto(primero));
        await usarViejo.Should().ThrowAsync<ValidationException>();
        await _service.RestablecerAsync(Dto(segundo));
    }

    [Fact]
    public async Task Respeta_la_espera_minima_entre_pedidos()
    {
        await _service.SolicitarAsync(Email);
        await _service.SolicitarAsync(Email);

        _email.Pedidos.Count(p => p.Evento == EventoNotificacion.RecuperarPassword).Should().Be(1);
    }

    [Fact]
    public async Task Un_token_de_confirmacion_no_sirve_para_cambiar_la_contraseña()
    {
        // Mismo mecanismo, distinto propósito: no son intercambiables.
        var token = TokenSeguro.Generar();
        _context.TokensVerificacion.Add(new TokenVerificacion
        {
            UsuarioId = 1, TokenHash = TokenSeguro.Hash(token), Proposito = PropositoToken.ConfirmacionEmail,
            FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddHours(1)
        });
        await _context.SaveChangesAsync();

        var usar = () => _service.RestablecerAsync(Dto(token));
        await usar.Should().ThrowAsync<ValidationException>();
    }
}
