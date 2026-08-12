using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Pacientes;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class PacienteServiceTests
{
    private readonly IPacienteService _service;
    private readonly VitalisDbContext _context;

    public PacienteServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new PacienteService(_context);
    }

    [Fact]
    public async Task CrearAsync_Should_Add_Paciente()
    {
        // Arrange
        var dto = new CrearPacienteDto
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "12345678",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Email = "juan@example.com",
            Telefono = "555-1234",
            Direccion = "Calle 1",
            ObraSocialId = null,
            NumeroAfiliado = null
        };

        // Act
        var result = await _service.CrearAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        var stored = await _context.Pacientes.FirstAsync();
        stored.Nombre.Should().Be(dto.Nombre);
        stored.Email.Should().Be(dto.Email);
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_ConflictException_When_Dni_Exists()
    {
        // Arrange
        var dto = new CrearPacienteDto
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "12345678",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Email = "juan@example.com",
            Telefono = "555-1234",
            Direccion = "Calle 1",
            ObraSocialId = null,
            NumeroAfiliado = null
        };

        // Create the first patient
        await _service.CrearAsync(dto);

        // Act & Assert
        // Creating another patient with same DNI should throw ConflictException
        var act = async () => await _service.CrearAsync(dto);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Ya existe un paciente registrado con el DNI ingresado.");
    }
}
