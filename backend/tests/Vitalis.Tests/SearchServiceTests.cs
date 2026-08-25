using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class SearchServiceTests
{
    private readonly ISearchService _service;
    private readonly VitalisDbContext _context;

    public SearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new SearchService(_context);

        SeedRelatedEntities();
    }

    private void SeedRelatedEntities()
    {
        _context.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE", Codigo = "OSDE", Activa = true });
        _context.Especialidades.Add(new Especialidad { Id = 1, Nombre = "Cardiologia", Descripcion = "Cardio" });

        _context.Profesionales.AddRange(
            new Profesional { Id = 1, Nombre = "Alejandro", Apellido = "Gomez", Matricula = "MP-1001", EspecialidadId = 1, Activo = true },
            new Profesional { Id = 2, Nombre = "Laura", Apellido = "Fernandez", Matricula = "MP-2002", EspecialidadId = 1, Activo = true }
        );

        _context.Pacientes.AddRange(
            new Paciente { Id = 1, Nombre = "Juan", Apellido = "Perez", Dni = "12345678", FechaNacimiento = new DateTime(1990, 1, 1), ObraSocialId = 1, Activo = true, FechaCreacion = DateTime.UtcNow },
            new Paciente { Id = 2, Nombre = "Maria", Apellido = "Lopez", Dni = "87654321", FechaNacimiento = new DateTime(1985, 5, 5), ObraSocialId = 1, Activo = true, FechaCreacion = DateTime.UtcNow }
        );

        _context.Turnos.Add(new Turno
        {
            Id = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = DateTime.SpecifyKind(new DateTime(2026, 1, 15, 10, 0, 0), DateTimeKind.Utc),
            Estado = "Confirmado",
            Confirmado = true
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task BuscarAsync_Should_Encontrar_Paciente_Por_Nombre()
    {
        var result = await _service.BuscarAsync("juan");

        result.Pacientes.Should().ContainSingle();
        result.Pacientes.Single().Titulo.Should().Be("Juan Perez");
        result.Pacientes.Single().Tipo.Should().Be("Paciente");
    }

    [Fact]
    public async Task BuscarAsync_Should_Encontrar_Paciente_Por_Dni()
    {
        var result = await _service.BuscarAsync("8765");

        result.Pacientes.Should().ContainSingle();
        result.Pacientes.Single().Titulo.Should().Be("Maria Lopez");
    }

    [Fact]
    public async Task BuscarAsync_Should_Encontrar_Profesional_Por_Apellido()
    {
        var result = await _service.BuscarAsync("fernandez");

        result.Profesionales.Should().ContainSingle();
        result.Profesionales.Single().Titulo.Should().Be("Laura Fernandez");
    }

    [Fact]
    public async Task BuscarAsync_Should_Encontrar_Profesional_Por_Matricula()
    {
        var result = await _service.BuscarAsync("2002");

        result.Profesionales.Should().ContainSingle();
        result.Profesionales.Single().Titulo.Should().Be("Laura Fernandez");
    }

    [Fact]
    public async Task BuscarAsync_Should_Encontrar_Turno_Por_Nombre_De_Paciente()
    {
        var result = await _service.BuscarAsync("juan");

        result.Turnos.Should().ContainSingle();
        result.Turnos.Single().Titulo.Should().Be("Juan Perez");
        result.Turnos.Single().Tipo.Should().Be("Turno");
    }

    [Fact]
    public async Task BuscarAsync_Should_Ser_Case_Insensitive()
    {
        var result = await _service.BuscarAsync("JUAN");

        result.Pacientes.Should().ContainSingle();
    }

    [Fact]
    public async Task BuscarAsync_Should_Retornar_Listas_Vacias_Cuando_No_Hay_Coincidencias()
    {
        var result = await _service.BuscarAsync("zzznomatch");

        result.Pacientes.Should().BeEmpty();
        result.Profesionales.Should().BeEmpty();
        result.Turnos.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarAsync_Should_Limitar_A_5_Resultados_Por_Categoria()
    {
        // El servicio usa Take(5) en cada categoría; se agregan 6 pacientes que matchean
        // "buscable" para confirmar que el límite se respeta.
        for (var i = 10; i <= 15; i++)
        {
            _context.Pacientes.Add(new Paciente
            {
                Id = i,
                Nombre = "Buscable",
                Apellido = $"Paciente{i}",
                Dni = $"{i}000000",
                FechaNacimiento = new DateTime(1990, 1, 1),
                ObraSocialId = 1,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var result = await _service.BuscarAsync("buscable");

        result.Pacientes.Should().HaveCount(5);
    }
}
