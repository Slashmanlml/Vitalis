using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Turnos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class NoOpEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
    public Task<IEnumerable<EmailLog>> GetEmailLogsAsync() => Task.FromResult<IEnumerable<EmailLog>>(new List<EmailLog>());
}

public class TurnoServiceTests
{
    private readonly ITurnoService _service;
    private readonly VitalisDbContext _context;

    public TurnoServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new TurnoService(_context, new NoOpEmailService());

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

    /// Mediodía local, 2 días hábiles hacia adelante: evita caer fuera de horario/fin de
    /// semana sin importar la zona horaria de la máquina que corre el test.
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
