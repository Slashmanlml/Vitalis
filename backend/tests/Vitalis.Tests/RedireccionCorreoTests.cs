using FluentAssertions;
using Vitalis.Infrastructure.Notificaciones;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

/// <summary>
/// En una demostración con correo real, los pacientes de prueba
/// (@email.com, un dominio real) no pueden recibir nada, pero quien se
/// registra con su propio correo sí.
/// </summary>
public class RedireccionCorreoTests
{
    private const string Mia = "yo@gmail.com";

    private static NotificacionesOptions Opciones(string redirigirA = "", string dominios = "") =>
        new() { RedirigirTodoA = redirigirA, RedirigirSoloDominios = dominios };

    [Fact]
    public void Sin_redireccion_va_al_destinatario()
    {
        EmailService.ResolverDestinatario(Opciones(), "profe@instituto.edu.ar")
            .Should().Be(("profe@instituto.edu.ar", false));
    }

    [Fact]
    public void Redireccion_sin_dominios_manda_todo_a_la_casilla_de_demo()
    {
        EmailService.ResolverDestinatario(Opciones(Mia), "profe@instituto.edu.ar")
            .Should().Be((Mia, true));
    }

    [Theory]
    [InlineData("lucasp@email.com")]
    [InlineData("admin@vitalis.local")]
    [InlineData("SofiaR@EMAIL.COM")]
    public void Los_dominios_de_prueba_se_redirigen(string destinatario)
    {
        EmailService.ResolverDestinatario(Opciones(Mia, "email.com, @vitalis.local"), destinatario)
            .Should().Be((Mia, true));
    }

    [Theory]
    [InlineData("profe@instituto.edu.ar")]
    [InlineData("alguien@gmail.com")]
    [InlineData("otro@mail.email.com")]
    public void Un_correo_real_llega_a_su_duenio(string destinatario)
    {
        EmailService.ResolverDestinatario(Opciones(Mia, "email.com,vitalis.local"), destinatario)
            .Should().Be((destinatario, false));
    }

    [Fact]
    public void Un_destinatario_sin_dominio_se_redirige_por_las_dudas()
    {
        EmailService.ResolverDestinatario(Opciones(Mia, "email.com"), "sin-arroba")
            .Should().Be((Mia, true));
    }
}
