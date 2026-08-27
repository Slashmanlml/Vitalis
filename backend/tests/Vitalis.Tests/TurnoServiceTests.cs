using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.DTOs.Turnos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class NoOpEmailService : IEmailService
{
    public bool DebeFallar { get; set; }
    public List<NotificacionRequest> NotificacionesEnviadas { get; } = new();

    public Task<bool> NotificarAsync(NotificacionRequest request)
    {
        if (DebeFallar) return Task.FromResult(false);
        NotificacionesEnviadas.Add(request);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<EmailLog>> GetEmailLogsAsync(string? origen = null, string? evento = null, string? estado = null) =>
        Task.FromResult<IEnumerable<EmailLog>>(new List<EmailLog>());

    public Task<EmailLog> SimularEnvioAsync(string to, string tipoNotificacion, string? asuntoPersonalizado = null, string? cuerpoPersonalizado = null) =>
        Task.FromResult(new EmailLog { Destinatario = to, Asunto = asuntoPersonalizado ?? tipoNotificacion, Cuerpo = cuerpoPersonalizado ?? "", Origen = OrigenNotificacion.Simulado, Estado = EstadoNotificacion.Simulado });

    public Task<bool> EliminarLogAsync(int id) => Task.FromResult(true);
}

public class TurnoServiceTests
{
    private readonly ITurnoService _service;
    private readonly VitalisDbContext _context;
    private readonly UsuarioActualDePrueba _usuarioActual = new();
    private readonly NoOpEmailService _emailService;

    public TurnoServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _emailService = new NoOpEmailService();
        _service = new TurnoService(_context, _emailService, _usuarioActual);

        SeedRelatedEntities();
    }

    private void SeedRelatedEntities()
    {
        var os = new ObraSocial { Id = 1, Nombre = "OSDE", Codigo = "OSDE", Activa = true };
        _context.ObrasSociales.Add(os);

        var pac = new Paciente
        {
            Id = 1,
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "12345678",
            Email = "juan.perez@test.com",
            FechaNacimiento = new DateTime(1990, 1, 1),
            ObraSocialId = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Pacientes.Add(pac);

        var esp = new Especialidad { Id = 1, Nombre = "Cardiologia", Descripcion = "Cardio" };
        _context.Especialidades.Add(esp);

        var prof = new Profesional
        {
            Id = 1,
            Nombre = "Alejandro",
            Apellido = "Gomez",
            Matricula = "MP-1001",
            EspecialidadId = 1,
            Activo = true
        };
        _context.Profesionales.Add(prof);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CrearAsync_Should_Add_Turno_When_Valid()
    {
        // Arrange
        var dto = new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = ProximoHorarioLaboralValido()
        };

        // Act
        var result = await _service.CrearAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Confirmado.Should().BeFalse();
        result.Estado.Should().Be("Solicitado");
    }

    [Fact]
    public async Task CrearAsync_GeneraNotificacionTurnoCreado()
    {
        // Arrange
        var dto = new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = ProximoHorarioLaboralValido()
        };

        // Act
        var result = await _service.CrearAsync(dto);

        // Assert
        _emailService.NotificacionesEnviadas.Should().ContainSingle();
        var notif = _emailService.NotificacionesEnviadas.First();
        notif.Evento.Should().Be(EventoNotificacion.TurnoCreado);
        notif.Destinatario.Should().Be("juan.perez@test.com");
        notif.TurnoId.Should().Be(result.Id);
    }

    [Fact]
    public async Task CrearAsync_CuandoEnvioNotificacionFalla_TurnoSeCreaIgual()
    {
        // Arrange
        _emailService.DebeFallar = true;
        var dto = new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = ProximoHorarioLaboralValido()
        };

        // Act
        var result = await _service.CrearAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        var enDb = await _context.Turnos.FindAsync(result.Id);
        enDb.Should().NotBeNull();
    }

    [Fact]
    public async Task EditarAsync_ConfirmarTurnoYaConfirmado_NoGeneraSegundoCorreo()
    {
        // Arrange
        var fecha = ProximoHorarioLaboralValido();
        var turno = await _service.CrearAsync(new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fecha
        });
        _emailService.NotificacionesEnviadas.Clear();

        // Act 1: Primera confirmación (false -> true)
        await _service.EditarAsync(turno.Id, new EditarTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fecha,
            Confirmado = true,
            Estado = "Confirmado"
        });

        _emailService.NotificacionesEnviadas.Should().ContainSingle(n => n.Evento == EventoNotificacion.TurnoConfirmado);
        _emailService.NotificacionesEnviadas.Clear();

        // Act 2: Segunda edición con Confirmado = true (sin transición)
        await _service.EditarAsync(turno.Id, new EditarTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fecha,
            Confirmado = true,
            Estado = "Confirmado"
        });

        // Assert
        _emailService.NotificacionesEnviadas.Should().BeEmpty();
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_ValidationException_When_FechaHora_In_Past()
    {
        // Arrange
        var dto = new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var act = async () => await _service.CrearAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("No se pueden agendar turnos en el pasado.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_ConflictException_When_Overlapping_Appointment_Exists()
    {
        // Arrange
        var fecha = ProximoHorarioLaboralValido();
        var dto1 = new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fecha
        };
        var dto2 = new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fecha
        };

        // Act & Assert
        await _service.CrearAsync(dto1);

        var act = async () => await _service.CrearAsync(dto2);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("El médico ya tiene asignado un turno en ese rango horario (se requiere un intervalo de 30 minutos).");
    }

    [Fact]
    public async Task EditarAsync_CambiarFecha_GeneraNotificacionDeReprogramacion()
    {
        // Arrange: un turno ya creado, y limpiamos el correo de creación
        var fechaOriginal = ProximoHorarioLaboralValido();
        var turno = await _service.CrearAsync(new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fechaOriginal
        });
        _emailService.NotificacionesEnviadas.Clear();

        var fechaNueva = fechaOriginal.AddHours(2);

        // Act
        await _service.EditarAsync(turno.Id, new EditarTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fechaNueva,
            Confirmado = false,
            Estado = "Solicitado"
        });

        // Assert: el evento debe ser Reprogramado, NO TurnoCreado. Antes de este
        // arreglo el paciente recibía un correo de "turno reservado con éxito"
        // cuando en realidad le habían movido la fecha.
        _emailService.NotificacionesEnviadas.Should().ContainSingle();
        var notif = _emailService.NotificacionesEnviadas.First();
        notif.Evento.Should().Be(EventoNotificacion.TurnoReprogramado);
        notif.TurnoId.Should().Be(turno.Id);
        notif.Datos.Should().ContainKey("FechaAnterior");
        notif.Datos!["FechaAnterior"].Should().Be(fechaOriginal.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
        notif.Datos["FechaHora"].Should().Be(fechaNueva.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
    }

    [Fact]
    public async Task EditarAsync_SinCambiarFecha_NoGeneraNotificacionDeReprogramacion()
    {
        // Arrange
        var fecha = ProximoHorarioLaboralValido();
        var turno = await _service.CrearAsync(new CrearTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fecha
        });
        _emailService.NotificacionesEnviadas.Clear();

        // Act: se edita el turno pero la fecha queda igual
        await _service.EditarAsync(turno.Id, new EditarTurnoDto
        {
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fecha,
            Confirmado = false,
            Estado = "Solicitado"
        });

        // Assert
        _emailService.NotificacionesEnviadas
            .Should().NotContain(n => n.Evento == EventoNotificacion.TurnoReprogramado);
    }

    // ---------------------------------------------------------------------
    // El filtrado por profesional se hacia en el navegador: el backend devolvia
    // la agenda completa de la clinica y el frontend escondia lo ajeno con un
    // .filter(). Los datos igual viajaban, y se veian abriendo las herramientas
    // de desarrollo. Estas pruebas fijan que el filtrado ocurra en el servidor.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ObtenerTodosAsync_Medico_SoloDevuelveSusPropiosTurnos()
    {
        // Arrange: dos profesionales con un turno cada uno
        _context.Profesionales.Add(new Profesional
        {
            Id = 2,
            Nombre = "Laura",
            Apellido = "Martinez",
            Matricula = "MP-1002",
            EspecialidadId = 1,
            Activo = true
        });
        _context.Turnos.AddRange(
            new Turno { Id = 901, PacienteId = 1, ProfesionalId = 1, ObraSocialId = 1, Estado = "Confirmado", FechaHora = DateTime.SpecifyKind(new DateTime(2026, 3, 10, 10, 0, 0), DateTimeKind.Utc) },
            new Turno { Id = 902, PacienteId = 1, ProfesionalId = 2, ObraSocialId = 1, Estado = "Confirmado", FechaHora = DateTime.SpecifyKind(new DateTime(2026, 3, 10, 11, 0, 0), DateTimeKind.Utc) }
        );
        await _context.SaveChangesAsync();

        _usuarioActual.Rol = Roles.Medico;
        _usuarioActual.ProfesionalId = 2;

        // Act
        var turnos = (await _service.ObtenerTodosAsync()).ToList();

        // Assert
        turnos.Should().OnlyContain(t => t.ProfesionalId == 2);
        turnos.Should().ContainSingle(t => t.Id == 902);
    }

    [Fact]
    public async Task ObtenerTodosAsync_Administrador_DevuelveTodaLaAgenda()
    {
        // Arrange
        _context.Profesionales.Add(new Profesional
        {
            Id = 2,
            Nombre = "Laura",
            Apellido = "Martinez",
            Matricula = "MP-1002",
            EspecialidadId = 1,
            Activo = true
        });
        _context.Turnos.AddRange(
            new Turno { Id = 901, PacienteId = 1, ProfesionalId = 1, ObraSocialId = 1, Estado = "Confirmado", FechaHora = DateTime.SpecifyKind(new DateTime(2026, 3, 10, 10, 0, 0), DateTimeKind.Utc) },
            new Turno { Id = 902, PacienteId = 1, ProfesionalId = 2, ObraSocialId = 1, Estado = "Confirmado", FechaHora = DateTime.SpecifyKind(new DateTime(2026, 3, 10, 11, 0, 0), DateTimeKind.Utc) }
        );
        await _context.SaveChangesAsync();

        _usuarioActual.Rol = Roles.Administrador;

        // Act
        var turnos = (await _service.ObtenerTodosAsync()).ToList();

        // Assert
        turnos.Should().HaveCountGreaterThanOrEqualTo(2);
        turnos.Should().Contain(t => t.ProfesionalId == 1);
        turnos.Should().Contain(t => t.ProfesionalId == 2);
    }

    [Fact]
    public async Task ObtenerTodosAsync_MedicoSinFichaProfesional_NoDevuelveNada()
    {
        // Arrange: ante una vinculacion faltante conviene una agenda vacia
        // antes que filtrar la de toda la clinica.
        _context.Turnos.Add(new Turno { Id = 901, PacienteId = 1, ProfesionalId = 1, ObraSocialId = 1, Estado = "Confirmado", FechaHora = DateTime.SpecifyKind(new DateTime(2026, 3, 10, 10, 0, 0), DateTimeKind.Utc) });
        await _context.SaveChangesAsync();

        _usuarioActual.Rol = Roles.Medico;
        _usuarioActual.ProfesionalId = null;

        // Act
        var turnos = (await _service.ObtenerTodosAsync()).ToList();

        // Assert
        turnos.Should().BeEmpty();
    }

    private static DateTime ProximoHorarioLaboralValido()
    {
        var fecha = DateTime.Now.Date.AddDays(2).AddHours(12);
        while (fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            fecha = fecha.AddDays(1);
        }
        return fecha.ToUniversalTime();
    }
}
