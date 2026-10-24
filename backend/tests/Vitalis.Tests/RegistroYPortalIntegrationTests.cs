using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Domain.Constants;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

/// <summary>
/// Flujo completo por HTTP: registrarse, no poder entrar, confirmar con el
/// enlace, entrar como paciente, ver lo propio y NO poder ver lo del consultorio.
/// </summary>
public class RegistroYPortalIntegrationTests : IClassFixture<TestFactory>
{
    private readonly TestFactory _factory;

    public RegistroYPortalIntegrationTests(TestFactory factory)
    {
        _factory = factory;
    }

    private static RegistroPacienteDto Dto(string email, string dni) => new()
    {
        Nombre = "Mario", Apellido = "Gómez", Dni = dni, FechaNacimiento = new DateTime(1988, 7, 1),
        Email = email, Password = "Mario12345", ConfirmarPassword = "Mario12345", AceptaTerminos = true
    };

    /// <summary>
    /// En las pruebas los correos no salen (modo prueba), así que el token se
    /// fabrica igual que lo haría el servicio y se guarda su hash: es lo mismo
    /// que hace la persona al abrir el enlace.
    /// </summary>
    private async Task<string> PlantarTokenAsync(string email, string proposito = PropositoToken.ConfirmacionEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<VitalisDbContext>();
        var usuario = await ctx.Usuarios.SingleAsync(u => u.Email == email);
        var token = TokenSeguro.Generar();
        ctx.TokensVerificacion.Add(new Vitalis.Domain.Entities.TokenVerificacion
        {
            UsuarioId = usuario.Id, TokenHash = TokenSeguro.Hash(token), Proposito = proposito,
            FechaCreacion = DateTime.UtcNow, FechaExpiracion = DateTime.UtcNow.AddHours(1)
        });
        await ctx.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task Flujo_completo_de_registro_confirmacion_y_portal()
    {
        var client = _factory.CreateClient();
        const string email = "mario.integracion@example.com";

        var registro = await client.PostAsJsonAsync("/api/auth/registro", Dto(email, "35123456"));
        registro.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var antes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = "Mario12345" });
        antes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await antes.Content.ReadAsStringAsync()).Should().Contain("EMAIL_NO_CONFIRMADO");

        var token = await PlantarTokenAsync(email);
        var confirmar = await client.PostAsJsonAsync("/api/auth/confirmar-email", new ConfirmarEmailDto { Token = token });
        confirmar.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = "Mario12345" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var sesion = await login.Content.ReadFromJsonAsync<LoginResponseDto>();
        sesion!.Rol.Should().Be(Roles.Paciente);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Token);

        (await client.GetAsync("/api/portal/mis-turnos")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/portal/mi-perfil")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Lo del consultorio, no.
        foreach (var ruta in new[] { "/api/Pacientes", "/api/Profesionales", "/api/Medicamentos", "/api/ObrasSociales",
                                     "/api/Especialidades", "/api/Prestaciones", "/api/Turnos", "/api/Usuarios", "/api/Auditorias" })
        {
            (await client.GetAsync(ruta)).StatusCode.Should().Be(HttpStatusCode.Forbidden, ruta);
        }
    }

    [Fact]
    public async Task Registro_con_contraseña_debil_se_rechaza()
    {
        var client = _factory.CreateClient();
        var dto = Dto("debil@example.com", "35123457");
        dto.Password = dto.ConfirmarPassword = "123456";

        (await client.PostAsJsonAsync("/api/auth/registro", dto)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task El_personal_no_entra_al_portal()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = "admin@vitalis.local", Password = "Admin123!" });
        var sesion = await login.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion!.Token);

        (await client.GetAsync("/api/portal/mis-turnos")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Las_respuestas_llevan_cabeceras_de_seguridad()
    {
        var client = _factory.CreateClient();
        var r = await client.GetAsync("/api/Health");

        r.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        r.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        r.Headers.Contains("Content-Security-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task Recuperar_la_contraseña_por_http()
    {
        var client = _factory.CreateClient();
        const string email = "rita.recupera@example.com";
        (await client.PostAsJsonAsync("/api/auth/registro", Dto(email, "36123457"))).StatusCode.Should().Be(HttpStatusCode.Accepted);
        var confirmacion = await PlantarTokenAsync(email);
        (await client.PostAsJsonAsync("/api/auth/confirmar-email", new ConfirmarEmailDto { Token = confirmacion })).EnsureSuccessStatusCode();

        // Pedir el enlace responde igual exista o no la cuenta.
        var existe = await client.PostAsJsonAsync("/api/auth/olvide-password", new SolicitarRecuperacionDto { Email = email });
        var noExiste = await client.PostAsJsonAsync("/api/auth/olvide-password", new SolicitarRecuperacionDto { Email = "nadie.nunca@example.com" });
        existe.StatusCode.Should().Be(HttpStatusCode.Accepted);
        noExiste.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await existe.Content.ReadAsStringAsync()).Should().Be(await noExiste.Content.ReadAsStringAsync());

        var token = await PlantarTokenAsync(email, PropositoToken.RecuperarPassword);

        var debil = await client.PostAsJsonAsync("/api/auth/restablecer-password",
            new RestablecerPasswordDto { Token = token, PasswordNuevo = "corta", ConfirmarPassword = "corta" });
        debil.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var ok = await client.PostAsJsonAsync("/api/auth/restablecer-password",
            new RestablecerPasswordDto { Token = token, PasswordNuevo = "Rita2026!x", ConfirmarPassword = "Rita2026!x" });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = "Rita2026!x" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = "Mario12345" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
