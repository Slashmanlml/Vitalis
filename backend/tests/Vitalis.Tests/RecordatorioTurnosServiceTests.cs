using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Notificaciones;
using Xunit;

namespace Vitalis.Tests;

public class MockEmailServiceForRecordatorios : IEmailService
{
    public List<NotificacionRequest> Notificaciones { get; } = new();
    public bool DebeFallar { get; set; }

    public Task<bool> NotificarAsync(NotificacionRequest request)
    {
        Notificaciones.Add(request);
        return Task.FromResult(!DebeFallar);
    }

    public Task<IEnumerable<EmailLog>> GetEmailLogsAsync(string? origen = null, string? evento = null, string? estado = null) =>
        Task.FromResult<IEnumerable<EmailLog>>(new List<EmailLog>());

    public Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion, string? asuntoPersonalizado = null, string? cuerpoPersonalizado = null) =>
        Task.FromResult(new EmailLog());

    public Task<bool> EliminarLogAsync(int id) => Task.FromResult(true);
}

public class RecordatorioTurnosServiceTests
{
    private readonly VitalisDbContext _context;
    private readonly MockEmailServiceForRecordatorios _emailService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificacionesOptions _options;

    public RecordatorioTurnosServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(dbOptions, new HttpContextAccessor());
        _emailService = new MockEmailServiceForRecordatorios();

        var services = new ServiceCollection();
        services.AddScoped(_ => _context);
        services.AddScoped<IEmailService>(_ => _emailService);
        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _options = new NotificacionesOptions
        {
            HorasAntesDelRecordatorio = 24,
            MinutosEntreBarridos = 30
        };

        SeedData();
    }

    private void SeedData()
    {
        _context.Pacientes.Add(new Paciente
        {
            Id = 1,
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "12345678",
            Email = "juan@test.com",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Activo = true
        });

        _context.Pacientes.Add(new Paciente
        {
            Id = 2,
            Nombre = "Sin",
            Apellido = "Email",
            Dni = "87654321",
            Email = null, // Sin email
            FechaNacimiento = new DateTime(1995, 5, 5),
            Activo = true
        });

        _context.Especialidades.Add(new Especialidad { Id = 1, Nombre = "Cardiología" });

        _context.Profesionales.Add(new Profesional
        {
            Id = 1,
            Nombre = "Alejandro",
            Apellido = "Gomez",
            Matricula = "MP-1001",
            EspecialidadId = 1,
            Activo = true
        });

        _context.SaveChanges();
    }

    private RecordatorioTurnosService CrearServicio()
    {
        return new RecordatorioTurnosService(_scopeFactory, Options.Create(_options), NullLogger<RecordatorioTurnosService>.Instance, new RelojDePrueba());
    }

    [Fact]
    public async Task ProcesarRecordatorios_TomaTurnoDentroDeLaVentana()
    {
        // Arrange: Turno en 24hs + 10min (dentro de la ventana [24h, 24h + 30m))
        var fecha = DateTime.UtcNow.AddHours(24).AddMinutes(10);
        _context.Turnos.Add(new Turno
        {
            Id = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            FechaHora = fecha,
            Estado = "Confirmado"
        });
        await _context.SaveChangesAsync();

        var service = CrearServicio();

        // Act
        var procesados = await service.ProcesarRecordatoriosAsync();

        // Assert
        procesados.Should().Be(1);
        _emailService.Notificaciones.Should().ContainSingle();
        _emailService.Notificaciones[0].Destinatario.Should().Be("juan@test.com");
        _emailService.Notificaciones[0].Evento.Should().Be(EventoNotificacion.RecordatorioTurno);
        _emailService.Notificaciones[0].TurnoId.Should().Be(1);
    }

    [Fact]
    public async Task ProcesarRecordatorios_NoTomaTurnoFueraDeLaVentana()
    {
        // Arrange: Turno en 26 horas (fuera de la ventana)
        _context.Turnos.Add(new Turno
        {
            Id = 2,
            PacienteId = 1,
            ProfesionalId = 1,
            FechaHora = DateTime.UtcNow.AddHours(26),
            Estado = "Confirmado"
        });
        await _context.SaveChangesAsync();

        var service = CrearServicio();

        // Act
        var procesados = await service.ProcesarRecordatoriosAsync();

        // Assert
        procesados.Should().Be(0);
        _emailService.Notificaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcesarRecordatorios_NoTomaTurnoCancelado()
    {
        // Arrange: Turno dentro de la ventana pero Cancelado
        _context.Turnos.Add(new Turno
        {
            Id = 3,
            PacienteId = 1,
            ProfesionalId = 1,
            FechaHora = DateTime.UtcNow.AddHours(24).AddMinutes(15),
            Estado = "Cancelado"
        });
        await _context.SaveChangesAsync();

        var service = CrearServicio();

        // Act
        var procesados = await service.ProcesarRecordatoriosAsync();

        // Assert
        procesados.Should().Be(0);
        _emailService.Notificaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcesarRecordatorios_NoVuelveATomarTurnoConRecordatorioEnviado()
    {
        // Arrange: Turno dentro de la ventana que ya tiene EmailLog "Enviado"
        _context.Turnos.Add(new Turno
        {
            Id = 4,
            PacienteId = 1,
            ProfesionalId = 1,
            FechaHora = DateTime.UtcNow.AddHours(24).AddMinutes(15),
            Estado = "Confirmado"
        });
        _context.EmailLogs.Add(new EmailLog
        {
            TurnoId = 4,
            Evento = EventoNotificacion.RecordatorioTurno,
            Estado = EstadoNotificacion.Enviado,
            Destinatario = "juan@test.com"
        });
        await _context.SaveChangesAsync();

        var service = CrearServicio();

        // Act
        var procesados = await service.ProcesarRecordatoriosAsync();

        // Assert (Idempotencia)
        procesados.Should().Be(0);
        _emailService.Notificaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcesarRecordatorios_SiVuelveATomarTurnoConRecordatorioFallido()
    {
        // Arrange: Turno dentro de la ventana que tuvo un intento "Fallido"
        _context.Turnos.Add(new Turno
        {
            Id = 5,
            PacienteId = 1,
            ProfesionalId = 1,
            FechaHora = DateTime.UtcNow.AddHours(24).AddMinutes(15),
            Estado = "Confirmado"
        });
        _context.EmailLogs.Add(new EmailLog
        {
            TurnoId = 5,
            Evento = EventoNotificacion.RecordatorioTurno,
            Estado = EstadoNotificacion.Fallido,
            Destinatario = "juan@test.com"
        });
        await _context.SaveChangesAsync();

        var service = CrearServicio();

        // Act
        var procesados = await service.ProcesarRecordatoriosAsync();

        // Assert: Reintenta enviar
        procesados.Should().Be(1);
        _emailService.Notificaciones.Should().ContainSingle();
        _emailService.Notificaciones[0].TurnoId.Should().Be(5);
    }

    [Fact]
    public async Task ProcesarRecordatorios_NoTomaTurnoDePacienteSinEmail()
    {
        // Arrange: Paciente 2 no tiene email
        _context.Turnos.Add(new Turno
        {
            Id = 6,
            PacienteId = 2,
            ProfesionalId = 1,
            FechaHora = DateTime.UtcNow.AddHours(24).AddMinutes(15),
            Estado = "Confirmado"
        });
        await _context.SaveChangesAsync();

        var service = CrearServicio();

        // Act
        var procesados = await service.ProcesarRecordatoriosAsync();

        // Assert
        procesados.Should().Be(0);
        _emailService.Notificaciones.Should().BeEmpty();
    }
}
