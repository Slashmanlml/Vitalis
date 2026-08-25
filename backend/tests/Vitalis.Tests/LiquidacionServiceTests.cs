using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Liquidaciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class LiquidacionServiceTests
{
    private readonly ILiquidacionService _service;
    private readonly VitalisDbContext _context;

    // Códigos de obra social usados por LiquidacionService.CrearAsync para calcular el
    // honorario del profesional (ver el switch en LiquidacionService.cs).
    private const int ObraSocialOsdeId = 1;   // $3200 base, 80% -> 2560
    private const int ObraSocialPamiId = 2;   // $2000 base, 70% -> 1400
    private const int ObraSocialSinMapeoId = 3; // código no contemplado -> cae al caso "Particular": $4000 base, 90% -> 3600

    private static readonly DateTime PeriodoDesde = new(2026, 1, 1);
    private static readonly DateTime PeriodoHasta = new(2026, 1, 31);

    public LiquidacionServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new LiquidacionService(_context);

        SeedRelatedEntities();
    }

    private void SeedRelatedEntities()
    {
        _context.ObrasSociales.AddRange(
            new ObraSocial { Id = ObraSocialOsdeId, Nombre = "OSDE", Codigo = "OSDE", Activa = true },
            new ObraSocial { Id = ObraSocialPamiId, Nombre = "PAMI", Codigo = "PAMI", Activa = true },
            new ObraSocial { Id = ObraSocialSinMapeoId, Nombre = "Particular", Codigo = "PARTICULAR", Activa = true }
        );

        var esp = new Especialidad { Id = 1, Nombre = "Cardiologia", Descripcion = "Cardio" };
        _context.Especialidades.Add(esp);

        _context.Profesionales.AddRange(
            new Profesional { Id = 1, Nombre = "Alejandro", Apellido = "Gomez", Matricula = "MP-1001", EspecialidadId = 1, Activo = true },
            new Profesional { Id = 2, Nombre = "Laura", Apellido = "Martinez", Matricula = "MP-1002", EspecialidadId = 1, Activo = true }
        );

        _context.Pacientes.Add(new Paciente
        {
            Id = 1,
            Nombre = "Juan",
            Apellido = "Perez",
            Dni = "12345678",
            FechaNacimiento = new DateTime(1990, 1, 1),
            ObraSocialId = ObraSocialOsdeId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        _context.SaveChanges();
    }

    private static Turno NuevoTurno(int id, int profesionalId, int obraSocialId, DateTime fechaHora, string estado)
    {
        return new Turno
        {
            Id = id,
            PacienteId = 1,
            ProfesionalId = profesionalId,
            ObraSocialId = obraSocialId,
            FechaHora = DateTime.SpecifyKind(fechaHora, DateTimeKind.Utc),
            Estado = estado,
            Confirmado = true
        };
    }

    [Fact]
    public async Task CrearAsync_Should_Apply_ObraSocial_Rate_From_Codigo()
    {
        _context.Turnos.Add(NuevoTurno(1, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 15), "Atendido"));
        await _context.SaveChangesAsync();

        var result = await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 1,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });

        // OSDE: $3200 base * 80% = 2560
        result.Total.Should().Be(2560m);
        result.Estado.Should().Be("Pendiente");
        result.ProfesionalNombre.Should().Be("Alejandro Gomez");
    }

    [Fact]
    public async Task CrearAsync_Should_Sum_Turnos_With_Distintas_Obras_Sociales()
    {
        _context.Turnos.AddRange(
            NuevoTurno(1, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 10), "Atendido"),
            NuevoTurno(2, profesionalId: 1, obraSocialId: ObraSocialPamiId, new DateTime(2026, 1, 20), "Atendido")
        );
        await _context.SaveChangesAsync();

        var result = await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 1,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });

        // OSDE (2560) + PAMI ($2000 * 70% = 1400)
        result.Total.Should().Be(3960m);
    }

    [Fact]
    public async Task CrearAsync_Should_Apply_Tarifa_Particular_When_Codigo_No_Mapeado()
    {
        _context.Turnos.Add(NuevoTurno(1, profesionalId: 1, obraSocialId: ObraSocialSinMapeoId, new DateTime(2026, 1, 15), "Atendido"));
        await _context.SaveChangesAsync();

        var result = await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 1,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });

        // Codigo "PARTICULAR" no está en el switch: cae al caso default ($4000 * 90% = 3600)
        result.Total.Should().Be(3600m);
    }

    [Fact]
    public async Task CrearAsync_Should_Excluir_Turnos_Fuera_Del_Periodo()
    {
        _context.Turnos.AddRange(
            NuevoTurno(1, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 15), "Atendido"),
            NuevoTurno(2, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 2, 15), "Atendido") // fuera de enero
        );
        await _context.SaveChangesAsync();

        var result = await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 1,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });

        result.Total.Should().Be(2560m); // solo el turno de enero
    }

    [Fact]
    public async Task CrearAsync_Should_Excluir_Turnos_De_Otro_Profesional()
    {
        _context.Turnos.AddRange(
            NuevoTurno(1, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 15), "Atendido"),
            NuevoTurno(2, profesionalId: 2, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 16), "Atendido")
        );
        await _context.SaveChangesAsync();

        var result = await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 1,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });

        result.Total.Should().Be(2560m); // no debe incluir el turno del profesional 2
    }

    [Fact]
    public async Task CrearAsync_Should_Excluir_Turnos_No_Atendidos()
    {
        // Nota: la condición real del servicio también incluye turnos con
        // ConsultaMedica != null aunque el estado no sea "Atendido"/"En Consulta". Ese
        // camino no se cubre acá porque depende de cómo el provider InMemory de EF Core
        // resuelve una navegación no incluida explícitamente (Include); se deja como
        // pendiente para una prueba de integración contra PostgreSQL real, siguiendo el
        // mismo criterio que ya usó el hardening previo (ver docs/07, sección 4).
        _context.Turnos.AddRange(
            NuevoTurno(1, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 10), "Solicitado"),
            NuevoTurno(2, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 20), "Atendido")
        );
        await _context.SaveChangesAsync();

        var result = await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 1,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });

        result.Total.Should().Be(2560m); // solo cuenta el turno "Atendido"
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_When_Profesional_No_Existe()
    {
        var act = async () => await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 999,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("Profesional no encontrado");
    }

    [Fact]
    public async Task LiquidarAsync_Should_Cambiar_Estado_A_Liquidada()
    {
        _context.Turnos.Add(NuevoTurno(1, profesionalId: 1, obraSocialId: ObraSocialOsdeId, new DateTime(2026, 1, 15), "Atendido"));
        await _context.SaveChangesAsync();

        var creada = await _service.CrearAsync(new CrearLiquidacionDto
        {
            ProfesionalId = 1,
            PeriodoDesde = PeriodoDesde,
            PeriodoHasta = PeriodoHasta
        });
        creada.Estado.Should().Be("Pendiente");

        var liquidada = await _service.LiquidarAsync(creada.Id);

        liquidada.Should().NotBeNull();
        liquidada!.Estado.Should().Be("Liquidada");
    }
}
