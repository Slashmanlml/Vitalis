using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Bloqueos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

/// Fake de IEmailService que registra cada envío, para poder afirmar en los tests que
/// BloqueoAgendaService notificó (o no) a los pacientes de los turnos cancelados.
/// NoOpEmailService (definida en TurnoServiceTests.cs, mismo namespace) no sirve acá porque
/// no deja rastro de qué se "envió".
public class RecordingEmailService : IEmailService
{
    public List<(string To, string Subject)> Enviados { get; } = new();

    public Task SendEmailAsync(string to, string subject, string body)
    {
        Enviados.Add((to, subject));
        return Task.CompletedTask;
    }

    public Task<IEnumerable<EmailLog>> GetEmailLogsAsync() => Task.FromResult<IEnumerable<EmailLog>>(new List<EmailLog>());
}

public class BloqueoAgendaServiceTests
{
    private readonly IBloqueoAgendaService _service;
    private readonly VitalisDbContext _context;
    private readonly RecordingEmailService _emailService;

    public BloqueoAgendaServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _emailService = new RecordingEmailService();
        _service = new BloqueoAgendaService(_context, _emailService);

        SeedRelatedEntities();
    }

    private void SeedRelatedEntities()
    {
        _context.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE", Codigo = "OSDE", Activa = true });
        _context.Especialidades.Add(new Especialidad { Id = 1, Nombre = "Cardiologia", Descripcion = "Cardio" });

        _context.Profesionales.Add(new Profesional
        {
            Id = 1,
            Nombre = "Alejandro",
            Apellido = "Gomez",
            Matricula = "MP-1001",
            EspecialidadId = 1,
            Activo = true
        });

        _context.Pacientes.Add(new Paciente
        {
            Id = 1,
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "12345678",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Email = "juan.perez@example.com",
            ObraSocialId = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    // Rango de bloqueo de referencia para los tests: mañana, de 10 a 12hs.
    private static (DateTime Inicio, DateTime Fin) RangoBloqueoValido()
    {
        var inicio = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(10), DateTimeKind.Utc);
        return (inicio, inicio.AddHours(2));
    }

    private Turno NuevoTurno(int id, DateTime fechaHora, string estado)
    {
        return new Turno
        {
            Id = id,
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = fechaHora,
            Estado = estado,
            Confirmado = true
        };
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_ValidationException_When_Inicio_No_Es_Anterior_A_Fin()
    {
        var (inicio, _) = RangoBloqueoValido();
        var dto = new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = inicio,
            FechaHoraFin = inicio, // igual al inicio: inválido
            Motivo = "Prueba"
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("La fecha de inicio debe ser anterior a la de fin.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_ValidationException_When_Inicio_Es_En_El_Pasado()
    {
        var dto = new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = DateTime.UtcNow.AddDays(-1),
            FechaHoraFin = DateTime.UtcNow.AddDays(-1).AddHours(2),
            Motivo = "Prueba"
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("No se pueden crear bloqueos en el pasado.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_When_Profesional_No_Existe()
    {
        var (inicio, fin) = RangoBloqueoValido();
        var dto = new CrearBloqueoDto
        {
            ProfesionalId = 9999,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Motivo = "Prueba"
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Profesional no encontrado.");
    }

    [Fact]
    public async Task CrearAsync_Should_Cancelar_Turnos_Superpuestos_Y_Notificar_Al_Paciente()
    {
        var (inicio, fin) = RangoBloqueoValido();
        _context.Turnos.Add(NuevoTurno(1, inicio.AddMinutes(30), "Confirmado")); // cae dentro del rango
        await _context.SaveChangesAsync();

        await _service.CrearAsync(new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Motivo = "Congreso médico"
        });

        var turno = await _context.Turnos.FindAsync(1);
        turno!.Estado.Should().Be("Cancelado");

        _emailService.Enviados.Should().ContainSingle();
        _emailService.Enviados[0].To.Should().Be("juan.perez@example.com");
    }

    [Fact]
    public async Task CrearAsync_Should_No_Afectar_Turnos_Fuera_Del_Rango_Bloqueado()
    {
        var (inicio, fin) = RangoBloqueoValido();
        _context.Turnos.Add(NuevoTurno(1, fin.AddHours(3), "Confirmado")); // fuera del rango
        await _context.SaveChangesAsync();

        await _service.CrearAsync(new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Motivo = "Congreso médico"
        });

        var turno = await _context.Turnos.FindAsync(1);
        turno!.Estado.Should().Be("Confirmado");
        _emailService.Enviados.Should().BeEmpty();
    }

    [Fact]
    public async Task CrearAsync_Should_No_Notificar_Turnos_Que_Ya_Estaban_Cancelados()
    {
        var (inicio, fin) = RangoBloqueoValido();
        _context.Turnos.Add(NuevoTurno(1, inicio.AddMinutes(30), "Cancelado")); // ya cancelado
        await _context.SaveChangesAsync();

        await _service.CrearAsync(new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Motivo = "Congreso médico"
        });

        _emailService.Enviados.Should().BeEmpty();
    }

    [Fact]
    public async Task EliminarAsync_Should_Remove_Y_Return_True_Cuando_Existe()
    {
        var (inicio, fin) = RangoBloqueoValido();
        var creado = await _service.CrearAsync(new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Motivo = "Congreso médico"
        });

        var eliminado = await _service.EliminarAsync(creado.Id);

        eliminado.Should().BeTrue();
        (await _service.ObtenerPorIdAsync(creado.Id)).Should().BeNull();
    }

    [Fact]
    public async Task EliminarAsync_Should_Return_False_Cuando_No_Existe()
    {
        var eliminado = await _service.EliminarAsync(9999);

        eliminado.Should().BeFalse();
    }

    [Fact]
    public async Task EsHorarioBloqueadoAsync_Should_Return_True_When_El_Turno_Se_Superpone_Con_Un_Bloqueo()
    {
        var (inicio, fin) = RangoBloqueoValido();
        await _service.CrearAsync(new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Motivo = "Congreso médico"
        });

        var resultado = await _service.EsHorarioBloqueadoAsync(1, inicio.AddMinutes(30));

        resultado.Should().BeTrue();
    }

    [Fact]
    public async Task EsHorarioBloqueadoAsync_Should_Return_False_When_No_Hay_Superposicion()
    {
        var (inicio, fin) = RangoBloqueoValido();
        await _service.CrearAsync(new CrearBloqueoDto
        {
            ProfesionalId = 1,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            Motivo = "Congreso médico"
        });

        var resultado = await _service.EsHorarioBloqueadoAsync(1, fin.AddHours(3));

        resultado.Should().BeFalse();
    }
}
