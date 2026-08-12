using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;
using Vitalis.Infrastructure.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Vitalis.Api;
using Vitalis.Application.DTOs.Auth;
using Vitalis.Application.DTOs.Pacientes;
using Xunit;

namespace Vitalis.Tests;

public class TestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<VitalisDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddDbContext<VitalisDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            services.AddControllers().AddApplicationPart(typeof(Program).Assembly);
        });
    }
}

public class PacientesControllerIntegrationTests : IClassFixture<TestFactory>
{
    private readonly HttpClient _client;

    public PacientesControllerIntegrationTests(TestFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CrearPacienteDto NuevoPacienteDto() => new()
    {
        Nombre = "Ana",
        Apellido = "Gomez",
        Dni = "87654321",
        FechaNacimiento = new DateTime(1985, 5, 10),
        Email = "ana@example.com",
        Telefono = "555-4321",
        Direccion = "Calle 2",
        ObraSocialId = null,
        NumeroAfiliado = null
    };

    [Fact]
    public async Task Post_Paciente_Without_Token_Returns_Unauthorized()
    {
        // Este test protege el hallazgo de seguridad corregido: crear un paciente
        // ya no puede hacerse de forma anónima.
        var response = await _client.PostAsJsonAsync("/api/Pacientes", NuevoPacienteDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_Paciente_With_Admin_Token_Returns_Created()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "admin@vitalis.local",
            Password = "Admin123!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);

        var response = await _client.PostAsJsonAsync("/api/Pacientes", NuevoPacienteDto());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PacienteDto>();
        created!.Nombre.Should().Be("Ana");
    }
}
