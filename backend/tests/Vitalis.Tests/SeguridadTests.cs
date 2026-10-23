using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Validation;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Configuracion;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

/// <summary>Pruebas de los hallazgos corregidos en la auditoría de seguridad.</summary>
public class SeguridadTests
{
    private const string Password = "Admin123!";

    private static VitalisDbContext NuevoContexto()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VitalisDbContext(options, new Microsoft.AspNetCore.Http.HttpContextAccessor());
    }

    private static (VitalisDbContext ctx, AuthService auth) ConUsuario(SeguridadOptions? opciones = null)
    {
        var ctx = NuevoContexto();
        ctx.Roles.Add(new Rol { Id = 1, Nombre = "Administrador" });
        ctx.Usuarios.Add(new Usuario
        {
            Id = 1, Nombre = "Admin", Apellido = "Vitalis", Email = "admin@vitalis.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), RolId = 1, Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        ctx.SaveChanges();
        return (ctx, new AuthService(ctx, new FakeJwtTokenService(), Options.Create(opciones ?? new SeguridadOptions())));
    }

    private static LoginRequestDto Login(string pass) => new() { Email = "admin@vitalis.local", Password = pass };

    // ---------- A07: fuerza bruta ----------

    [Fact]
    public async Task Cinco_contraseñas_incorrectas_bloquean_la_cuenta()
    {
        var (ctx, auth) = ConUsuario();

        for (var i = 0; i < 4; i++)
            (await auth.LoginAsync(Login("mal"))).Should().BeNull();

        var quinto = () => auth.LoginAsync(Login("mal"));
        (await quinto.Should().ThrowAsync<DemasiadosIntentosException>()).Which.Codigo.Should().Be(AuthService.CodigoCuentaBloqueada);

        (await ctx.Usuarios.SingleAsync()).BloqueadoHasta.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Durante_el_bloqueo_ni_la_contraseña_correcta_entra()
    {
        var (ctx, auth) = ConUsuario();
        var u = await ctx.Usuarios.SingleAsync();
        u.BloqueadoHasta = DateTime.UtcNow.AddMinutes(10);
        await ctx.SaveChangesAsync();

        var correcto = () => auth.LoginAsync(Login(Password));
        await correcto.Should().ThrowAsync<DemasiadosIntentosException>();
    }

    [Fact]
    public async Task Pasado_el_bloqueo_se_puede_entrar_y_se_reinician_los_intentos()
    {
        var (ctx, auth) = ConUsuario();
        var u = await ctx.Usuarios.SingleAsync();
        u.BloqueadoHasta = DateTime.UtcNow.AddMinutes(-1);
        u.IntentosFallidos = 3;
        await ctx.SaveChangesAsync();

        (await auth.LoginAsync(Login(Password))).Should().NotBeNull();

        var despues = await ctx.Usuarios.SingleAsync();
        despues.IntentosFallidos.Should().Be(0);
        despues.BloqueadoHasta.Should().BeNull();
        despues.UltimoAcceso.Should().NotBeNull();
    }

    [Fact]
    public async Task El_correo_no_distingue_mayusculas_ni_espacios()
    {
        var (_, auth) = ConUsuario();
        (await auth.LoginAsync(new LoginRequestDto { Email = "  ADMIN@Vitalis.Local ", Password = Password })).Should().NotBeNull();
    }

    // ---------- A07: política de contraseñas ----------

    [Theory]
    [InlineData("Abc12345", true)]
    [InlineData("Paciente123", true)]
    [InlineData("abc12345", false)]   // sin mayúscula
    [InlineData("ABC12345", false)]   // sin minúscula
    [InlineData("Abcdefgh", false)]   // sin número
    [InlineData("Ab1", false)]        // corta
    [InlineData("123456", false)]     // la que antes alcanzaba (MinLength 6)
    [InlineData("", false)]
    public void Politica_de_contraseñas(string valor, bool esperado)
    {
        PasswordSeguraAttribute.EsSegura(valor).Should().Be(esperado);
    }

    // ---------- A02/A09: secretos en la auditoría ----------

    [Fact]
    public async Task La_auditoria_no_guarda_el_hash_de_la_contraseña()
    {
        var (ctx, _) = ConUsuario();

        var u = await ctx.Usuarios.SingleAsync();
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword("OtraClave123");
        await ctx.SaveChangesAsync();

        var registros = (await ctx.Auditorias.ToListAsync()).Where(a => a.Tabla.Contains("usuario", StringComparison.OrdinalIgnoreCase) || a.Tabla == "Usuario").ToList();
        registros.Should().NotBeEmpty();
        foreach (var a in registros)
        {
            (a.ValoresNuevos ?? "").Should().NotContain("$2");   // prefijo de un hash bcrypt
            (a.ValoresAnteriores ?? "").Should().NotContain("$2");
        }
        registros.Last().ValoresNuevos.Should().Contain(VitalisDbContext.ValorOculto);
    }

    // ---------- A02: el enlace de confirmación no queda en el historial ----------

    [Fact]
    public void El_enlace_sensible_se_oculta_en_el_cuerpo_que_se_guarda()
    {
        const string url = "http://localhost:4200/confirmar-email?token=abc&x=1";
        var pedido = new NotificacionRequest
        {
            Datos = new Dictionary<string, string> { ["UrlConfirmacion"] = url },
            DatosSensibles = ["UrlConfirmacion"]
        };
        var cuerpo = $"<a href='{System.Net.WebUtility.HtmlEncode(url)}'>ir</a> {url}";

        var guardado = EmailService.OcultarDatosSensibles(cuerpo, pedido);

        guardado.Should().NotContain("token=abc");
        guardado.Should().Contain("[enlace oculto por seguridad]");
    }

    [Fact]
    public void Generar_token_da_valores_distintos_y_largos()
    {
        var tokens = Enumerable.Range(0, 50).Select(_ => TokenSeguro.Generar()).ToList();
        tokens.Distinct().Should().HaveCount(50);
        tokens.Should().OnlyContain(t => t.Length >= 43 && !t.Contains('+') && !t.Contains('/') && !t.Contains('='));
        TokenSeguro.Hash("x").Should().HaveLength(64);
    }
}
