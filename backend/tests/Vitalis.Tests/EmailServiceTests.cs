using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Notificaciones;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class ClienteSmtpFalso : IClienteSmtp
{
    public bool DebeFallar { get; set; }
    public string MensajeFallo { get; set; } = "Error de conexión SMTP simulado";
    public List<(string RemitenteNombre, string RemitenteEmail, string Destinatario, string Asunto, string CuerpoHtml)> Enviados { get; } = new();

    public Task EnviarAsync(string remitenteNombre, string remitenteEmail, string destinatario, string asunto, string cuerpoHtml)
    {
        if (DebeFallar)
        {
            throw new Exception(MensajeFallo);
        }

        Enviados.Add((remitenteNombre, remitenteEmail, destinatario, asunto, cuerpoHtml));
        return Task.CompletedTask;
    }
}

public class EmailServiceTests
{
    private readonly VitalisDbContext _context;
    private readonly ClienteSmtpFalso _smtpClient;
    private readonly NotificacionesOptions _options;

    public EmailServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(dbOptions, new HttpContextAccessor());
        _smtpClient = new ClienteSmtpFalso();
        _options = new NotificacionesOptions
        {
            Habilitado = true,
            ModoPrueba = false,
            RedirigirTodoA = "",
            RemitenteNombre = "Vitalis",
            RemitenteEmail = "no-responder@vitalis.local"
        };
    }

    private IEmailService CrearServicio(NotificacionesOptions? options = null)
    {
        var opt = Options.Create(options ?? _options);
        return new EmailService(_context, _smtpClient, opt, NullLogger<EmailService>.Instance);
    }

    [Fact]
    public async Task NotificarAsync_RegistraConOrigenSistemaYEventoRecibido()
    {
        // Arrange
        var service = CrearServicio();
        var request = new NotificacionRequest
        {
            Destinatario = "paciente@test.com",
            Evento = EventoNotificacion.TurnoCreado,
            Datos = new Dictionary<string, string>
            {
                ["PacienteNombre"] = "Juan Perez",
                ["ProfesionalNombre"] = "Dr. Gomez",
                ["FechaHora"] = "28/08/2026 10:00",
                ["Especialidad"] = "Cardiología"
            }
        };

        // Act
        var result = await service.NotificarAsync(request);

        // Assert
        result.Should().BeTrue();
        _smtpClient.Enviados.Should().ContainSingle();
        _smtpClient.Enviados[0].Destinatario.Should().Be("paciente@test.com");

        var logs = await service.GetEmailLogsAsync();
        logs.Should().ContainSingle();
        var log = logs.First();
        log.Destinatario.Should().Be("paciente@test.com");
        log.Origen.Should().Be(OrigenNotificacion.Sistema);
        log.Evento.Should().Be(EventoNotificacion.TurnoCreado);
        log.Estado.Should().Be(EstadoNotificacion.Enviado);
        log.Cuerpo.Should().Contain("Juan Perez");
    }

    [Fact]
    public async Task NotificarAsync_ConModoPruebaTrue_RegistraEstadoSimuladoYNoInvocaSmtp()
    {
        // Arrange
        _options.ModoPrueba = true;
        var service = CrearServicio(_options);
        var request = new NotificacionRequest
        {
            Destinatario = "paciente@test.com",
            Evento = EventoNotificacion.RecordatorioTurno,
            Datos = new Dictionary<string, string> { ["PacienteNombre"] = "Ana" }
        };

        // Act
        var result = await service.NotificarAsync(request);

        // Assert
        result.Should().BeTrue();
        _smtpClient.Enviados.Should().BeEmpty(); // No invoca SMTP

        var logs = await service.GetEmailLogsAsync();
        logs.Should().ContainSingle();
        logs.First().Estado.Should().Be(EstadoNotificacion.Simulado);
        logs.First().Origen.Should().Be(OrigenNotificacion.Sistema);
    }

    [Fact]
    public async Task NotificarAsync_CuandoSmtpFalla_NoLanzaYRegistraEstadoFallidoConMensajeError()
    {
        // Arrange
        _smtpClient.DebeFallar = true;
        _smtpClient.MensajeFallo = "Conexión rechazada por el servidor SMTP";
        var service = CrearServicio();
        var request = new NotificacionRequest
        {
            Destinatario = "paciente@test.com",
            Evento = EventoNotificacion.TurnoCancelado,
            Datos = new Dictionary<string, string>()
        };

        // Act
        var act = async () => await service.NotificarAsync(request);

        // Assert
        var result = await act.Should().NotThrowAsync();
        result.Subject.Should().BeFalse();

        var logs = await service.GetEmailLogsAsync();
        logs.Should().ContainSingle();
        var log = logs.First();
        log.Estado.Should().Be(EstadoNotificacion.Fallido);
        log.MensajeError.Should().Contain("Conexión rechazada");
    }

    [Fact]
    public async Task NotificarAsync_ConRedirigirTodoA_RedirigeDestinatarioRealPeroConservaOriginalEnLog()
    {
        // Arrange
        _options.RedirigirTodoA = "auditoria@vitalis.local";
        var service = CrearServicio(_options);
        var request = new NotificacionRequest
        {
            Destinatario = "paciente.real@correo.com",
            Evento = EventoNotificacion.TurnoConfirmado,
            Datos = new Dictionary<string, string> { ["PacienteNombre"] = "Carlos" }
        };

        // Act
        await service.NotificarAsync(request);

        // Assert
        _smtpClient.Enviados.Should().ContainSingle();
        _smtpClient.Enviados[0].Destinatario.Should().Be("auditoria@vitalis.local");
        _smtpClient.Enviados[0].Asunto.Should().Contain("[Para: paciente.real@correo.com]");

        var logs = await service.GetEmailLogsAsync();
        logs.First().Destinatario.Should().Be("paciente.real@correo.com"); // Conserva original
    }

    [Fact]
    public async Task SimularEnvioAsync_SiempreFuerzaOrigenSimuladoYEstadoSimulado()
    {
        // Arrange
        var service = CrearServicio();

        // Act
        var log = await service.SimularEnvioAsync("demo@test.com", EventoNotificacion.NuevaPrescripcion);

        // Assert
        log.Origen.Should().Be(OrigenNotificacion.Simulado);
        log.Estado.Should().Be(EstadoNotificacion.Simulado);
        log.Evento.Should().Be(EventoNotificacion.NuevaPrescripcion);
        _smtpClient.Enviados.Should().BeEmpty();
    }

    [Fact]
    public async Task EliminarLogAsync_OrigenSistema_LanzaConflictException()
    {
        // Arrange
        var service = CrearServicio();
        await service.NotificarAsync(new NotificacionRequest
        {
            Destinatario = "sistema@test.com",
            Evento = EventoNotificacion.TurnoCreado
        });

        var log = (await service.GetEmailLogsAsync()).First();

        // Act
        var act = async () => await service.EliminarLogAsync(log.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("No se pueden eliminar notificaciones emitidas por el sistema.");
    }

    [Fact]
    public async Task EliminarLogAsync_OrigenSimulado_EliminaRegistroCorrectamente()
    {
        // Arrange
        var service = CrearServicio();
        var log = await service.SimularEnvioAsync("simulado@test.com", EventoNotificacion.Personalizado);

        // Act
        var result = await service.EliminarLogAsync(log.Id);

        // Assert
        result.Should().BeTrue();
        (await service.GetEmailLogsAsync()).Should().BeEmpty();
    }
}
