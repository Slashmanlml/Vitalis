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

    // -------------------------------------------------------------------------
    // Baja lógica y reactivación.
    //
    // El botón de eliminar del listado no borra: llama a DesactivarAsync, que
    // pone Activo en false, y el listado filtra por Activo. Eso está bien —la
    // historia clínica del paciente debe conservarse— pero hasta esta versión no
    // había forma de ver ni recuperar a un paciente dado de baja: desaparecía de
    // la interfaz de manera permanente. Detectado probando el sistema a mano.
    // -------------------------------------------------------------------------

    private async Task<PacienteDto> NuevoPaciente(string dni)
    {
        return await _service.CrearAsync(new CrearPacienteDto
        {
            Nombre = "Ana",
            Apellido = "Gomez",
            Dni = dni,
            FechaNacimiento = new DateTime(1990, 1, 1),
            Email = $"ana{dni}@example.com",
            Telefono = "555-0000",
            Direccion = "Calle 1"
        });
    }

    [Fact]
    public async Task ObtenerTodosAsync_PorOmision_NoDevuelveLosDadosDeBaja()
    {
        var activo = await NuevoPaciente("11111111");
        var baja = await NuevoPaciente("22222222");
        await _service.DesactivarAsync(baja.Id);

        var listado = await _service.ObtenerTodosAsync();

        listado.Should().Contain(p => p.Id == activo.Id);
        listado.Should().NotContain(p => p.Id == baja.Id);
    }

    [Fact]
    public async Task ObtenerTodosAsync_ConIncluirInactivos_DevuelveTambienLosDadosDeBaja()
    {
        var activo = await NuevoPaciente("33333333");
        var baja = await NuevoPaciente("44444444");
        await _service.DesactivarAsync(baja.Id);

        var listado = await _service.ObtenerTodosAsync(incluirInactivos: true);

        listado.Should().Contain(p => p.Id == activo.Id);
        listado.Should().Contain(p => p.Id == baja.Id);
    }

    [Fact]
    public async Task ReactivarAsync_DevuelveElPacienteAlListado()
    {
        var paciente = await NuevoPaciente("55555555");
        await _service.DesactivarAsync(paciente.Id);
        (await _service.ObtenerTodosAsync()).Should().NotContain(p => p.Id == paciente.Id);

        var resultado = await _service.ReactivarAsync(paciente.Id);

        resultado.Should().BeTrue();
        (await _service.ObtenerTodosAsync()).Should().Contain(p => p.Id == paciente.Id);
    }

    [Fact]
    public async Task ReactivarAsync_PacienteInexistente_DevuelveFalse()
    {
        (await _service.ReactivarAsync(9999)).Should().BeFalse();
    }

    [Fact]
    public async Task DesactivarAsync_ConservaElRegistro_NoLoElimina()
    {
        // Lo importante de una baja lógica: la fila sigue en la base, con toda su
        // historia clínica asociada. Si se borrara, se perderían las consultas.
        var paciente = await NuevoPaciente("66666666");

        await _service.DesactivarAsync(paciente.Id);

        (await _context.Pacientes.FindAsync(paciente.Id)).Should().NotBeNull();
    }
}
