using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class EmailServiceTests
{
    private readonly IEmailService _service;
    private readonly VitalisDbContext _context;

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new EmailService(_context);
    }

    [Fact]
    public async Task SendEmailAsync_GuardaRegistroEnBaseDeDatos()
    {
        // Act
        await _service.SendEmailAsync("paciente@test.com", "Asunto Prueba", "<p>Cuerpo del correo</p>");

        // Assert
        var logs = await _service.GetEmailLogsAsync();
        logs.Should().ContainSingle();
        var log = logs.First();
        log.Destinatario.Should().Be("paciente@test.com");
        log.Asunto.Should().Be("Asunto Prueba");
        log.Cuerpo.Should().Contain("Cuerpo del correo");
    }

    [Fact]
    public async Task GetEmailLogsAsync_OrdenaPorFechaDescendente()
    {
        // Arrange
        await _service.SendEmailAsync("paciente1@test.com", "Email 1", "Body 1");
        await Task.Delay(10);
        await _service.SendEmailAsync("paciente2@test.com", "Email 2", "Body 2");

        // Act
        var logs = (await _service.GetEmailLogsAsync()).ToList();

        // Assert
        logs.Should().HaveCount(2);
        logs[0].Destinatario.Should().Be("paciente2@test.com");
        logs[1].Destinatario.Should().Be("paciente1@test.com");
    }

    [Fact]
    public async Task SimularEnvioAsync_ConPlantillaConfirmacionTurno_GeneraAsuntoYCuerpo()
    {
        // Act
        var log = await _service.SimularEnvioAsync("paciente@vitalis.local", "ConfirmacionTurno");

        // Assert
        log.Should().NotBeNull();
        log.Destinatario.Should().Be("paciente@vitalis.local");
        log.Asunto.Should().Contain("Confirmación de Turno");
        log.Cuerpo.Should().Contain("Turno Confirmado con Éxito");

        var enDb = await _context.EmailLogs.FindAsync(log.Id);
        enDb.Should().NotBeNull();
    }

    [Fact]
    public async Task SimularEnvioAsync_ConPlantillaPrescripcion_GeneraAsuntoYCuerpo()
    {
        // Act
        var log = await _service.SimularEnvioAsync("paciente@vitalis.local", "NuevaPrescripcion");

        // Assert
        log.Should().NotBeNull();
        log.Asunto.Should().Contain("Receta Médica");
        log.Cuerpo.Should().Contain("Receta Médica Electrónica");
    }

    [Fact]
    public async Task EliminarLogAsync_Existente_LoElimina()
    {
        // Arrange
        await _service.SendEmailAsync("eliminar@test.com", "Asunto", "Body");
        var log = (await _service.GetEmailLogsAsync()).First();

        // Act
        var result = await _service.EliminarLogAsync(log.Id);

        // Assert
        result.Should().BeTrue();
        var logsRestantes = await _service.GetEmailLogsAsync();
        logsRestantes.Should().BeEmpty();
    }

    [Fact]
    public async Task LimpiarLogsAsync_BorraTodosLosRegistros()
    {
        // Arrange
        await _service.SendEmailAsync("mail1@test.com", "A1", "B1");
        await _service.SendEmailAsync("mail2@test.com", "A2", "B2");
        await _service.SendEmailAsync("mail3@test.com", "A3", "B3");

        // Act
        var result = await _service.LimpiarLogsAsync();

        // Assert
        result.Should().BeTrue();
        (await _service.GetEmailLogsAsync()).Should().BeEmpty();
    }
}
