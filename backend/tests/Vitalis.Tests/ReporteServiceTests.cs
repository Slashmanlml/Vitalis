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

public class ReporteServiceTests
{
    private readonly IReporteService _service;
    private readonly VitalisDbContext _context;

    public ReporteServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new ReporteService(_context);

        SeedRelatedEntities();
    }

    private void SeedRelatedEntities()
    {
        _context.ObrasSociales.AddRange(
            new ObraSocial { Id = 1, Nombre = "OSDE", Codigo = "OSDE", Activa = true },
            new ObraSocial { Id = 2, Nombre = "PAMI", Codigo = "PAMI", Activa = true }
        );

        _context.Especialidades.AddRange(
            new Especialidad { Id = 1, Nombre = "Cardiologia", Descripcion = "Cardio" },
            new Especialidad { Id = 2, Nombre = "Pediatria", Descripcion = "Pedia" }
        );

        // Ojo: los profesionales 1 y 2 comparten especialidad. Es intencional,
        // porque es el caso que destapa el agrupamiento incorrecto (ver el test
        // PorEspecialidad_Should_Agrupar_Por_Especialidad_Y_No_Por_Profesional).
        _context.Profesionales.AddRange(
            new Profesional { Id = 1, Nombre = "Alejandro", Apellido = "Gomez", Matricula = "MP-1001", EspecialidadId = 1, Activo = true },
            new Profesional { Id = 2, Nombre = "Laura", Apellido = "Diaz", Matricula = "MP-2002", EspecialidadId = 1, Activo = true },
            new Profesional { Id = 3, Nombre = "Sergio", Apellido = "Ruiz", Matricula = "MP-3003", EspecialidadId = 2, Activo = true }
        );

        _context.Pacientes.AddRange(
            new Paciente { Id = 1, Nombre = "Juan", Apellido = "Perez", Dni = "12345678", FechaNacimiento = new DateTime(1990, 1, 1), ObraSocialId = 1, Activo = true, FechaCreacion = DateTime.UtcNow },
            new Paciente { Id = 2, Nombre = "Maria", Apellido = "Lopez", Dni = "87654321", FechaNacimiento = new DateTime(1985, 5, 5), ObraSocialId = 2, Activo = true, FechaCreacion = DateTime.UtcNow }
        );

        void Turno_(int id, int pac, int prof, int os, DateTime fecha, string estado, bool confirmado) =>
            _context.Turnos.Add(new Turno
            {
                Id = id,
                PacienteId = pac,
                ProfesionalId = prof,
                ObraSocialId = os,
                FechaHora = DateTime.SpecifyKind(fecha, DateTimeKind.Utc),
                Estado = estado,
                Confirmado = confirmado
            });

        Turno_(1, 1, 1, 1, new DateTime(2026, 1, 10, 9, 0, 0), "Confirmado", true);
        Turno_(2, 1, 1, 1, new DateTime(2026, 1, 20, 9, 0, 0), "Solicitado", false);
        Turno_(3, 2, 2, 2, new DateTime(2026, 1, 15, 9, 0, 0), "Confirmado", true);
        Turno_(4, 2, 3, 2, new DateTime(2026, 2, 5, 9, 0, 0), "Atendido", true);
        Turno_(5, 1, 1, 1, new DateTime(2026, 2, 10, 9, 0, 0), "Cancelado", false);

        _context.SaveChanges();
    }

    // ------------------------------------------------------------ listados

    [Fact]
    public async Task TurnosPorProfesionalAsync_Should_Retornar_Solo_Los_Turnos_Del_Profesional()
    {
        var result = await _service.TurnosPorProfesionalAsync(1, null, null);

        result.Should().HaveCount(3);
        result.Select(t => t.Id).Should().BeEquivalentTo(new[] { 1, 2, 5 });
    }

    [Fact]
    public async Task TurnosPorProfesionalAsync_Should_Filtrar_Por_Rango_De_Fechas()
    {
        var desde = DateTime.SpecifyKind(new DateTime(2026, 1, 15), DateTimeKind.Utc);
        var hasta = DateTime.SpecifyKind(new DateTime(2026, 1, 25), DateTimeKind.Utc);

        var result = await _service.TurnosPorProfesionalAsync(1, desde, hasta);

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(2);
    }

    [Fact]
    public async Task TurnosPorProfesionalAsync_Should_Ordenar_Del_Mas_Reciente_Al_Mas_Viejo()
    {
        var result = (await _service.TurnosPorProfesionalAsync(1, null, null)).ToList();

        result.Select(t => t.Id).Should().ContainInOrder(5, 2, 1);
    }

    [Fact]
    public async Task TurnosPorPacienteAsync_Should_Retornar_Turnos_Del_Paciente()
    {
        var result = await _service.TurnosPorPacienteAsync(1);

        result.Should().HaveCount(3);
        result.Should().OnlyContain(t => t.PacienteId == 1);
    }

    [Fact]
    public async Task TurnosPorObraSocialAsync_Should_Retornar_Turnos_De_La_Obra_Social()
    {
        var result = await _service.TurnosPorObraSocialAsync(2);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.ObraSocialNombre == "PAMI");
    }

    // --------------------------------------------- regresiones de proyección

    [Fact]
    public async Task Los_Reportes_Should_Incluir_Nombre_Y_Apellido_Igual_Que_El_Resto_Del_Sistema()
    {
        // Regresión: los reportes proyectaban sólo Paciente.Nombre / Profesional.Nombre,
        // por lo que un mismo turno aparecía como "Juan" en un reporte y como
        // "Juan Perez" en la agenda.
        var result = await _service.TurnosPorPacienteAsync(1);
        var turno = result.First(t => t.Id == 1);

        turno.PacienteNombre.Should().Be("Juan Perez");
        turno.ProfesionalNombre.Should().Be("Alejandro Gomez");
    }

    [Fact]
    public async Task Los_Reportes_Should_Conservar_El_Estado_Real_Del_Turno()
    {
        // Regresión: la proyección nunca asignaba Estado, así que TurnoDto caía
        // en su valor por defecto y TODOS los turnos de cualquier reporte
        // figuraban como "Solicitado".
        var result = (await _service.TurnosPorProfesionalAsync(1, null, null)).ToList();

        result.Single(t => t.Id == 1).Estado.Should().Be("Confirmado");
        result.Single(t => t.Id == 2).Estado.Should().Be("Solicitado");
        result.Single(t => t.Id == 5).Estado.Should().Be("Cancelado");
    }

    // ------------------------------------------------------- estadísticas

    [Fact]
    public async Task EstadisticasGeneralesAsync_Should_Contar_Total_Confirmados_Y_Pendientes()
    {
        var result = await _service.EstadisticasGeneralesAsync();

        result.TotalTurnos.Should().Be(5);
        result.Confirmados.Should().Be(3);
        result.Pendientes.Should().Be(2);
    }

    [Fact]
    public async Task EstadisticasGeneralesAsync_Should_Contar_Atendidos_Y_Cancelados()
    {
        var result = await _service.EstadisticasGeneralesAsync();

        result.Atendidos.Should().Be(1);
        result.Cancelados.Should().Be(1);
    }

    [Fact]
    public async Task PorEspecialidad_Should_Agrupar_Por_Especialidad_Y_No_Por_Profesional()
    {
        // Regresión: el GroupJoin anterior emitía una fila por profesional
        // etiquetada con su especialidad, de modo que Cardiologia aparecía dos
        // veces (una por cada cardiólogo) en lugar de sumarse.
        var result = await _service.EstadisticasGeneralesAsync();

        result.PorEspecialidad.Should().HaveCount(2);
        result.PorEspecialidad.Should().ContainSingle(x => x.Etiqueta == "Cardiologia")
            .Which.Cantidad.Should().Be(4);
        result.PorEspecialidad.Should().ContainSingle(x => x.Etiqueta == "Pediatria")
            .Which.Cantidad.Should().Be(1);
    }

    [Fact]
    public async Task PorObraSocial_Should_Contar_Los_Turnos_De_Cada_Obra_Social()
    {
        var result = await _service.EstadisticasGeneralesAsync();

        result.PorObraSocial.Single(x => x.Etiqueta == "OSDE").Cantidad.Should().Be(3);
        result.PorObraSocial.Single(x => x.Etiqueta == "PAMI").Cantidad.Should().Be(2);
    }

    [Fact]
    public async Task PorProfesional_Should_Usar_Nombre_Completo_Y_Ordenar_Por_Cantidad()
    {
        var result = await _service.EstadisticasGeneralesAsync();

        result.PorProfesional.First().Etiqueta.Should().Be("Alejandro Gomez");
        result.PorProfesional.First().Cantidad.Should().Be(3);
    }

    [Fact]
    public async Task PorMes_Should_Devolver_La_Serie_En_Orden_Cronologico()
    {
        var result = await _service.EstadisticasGeneralesAsync();

        result.PorMes.Select(x => x.Etiqueta).Should().ContainInOrder("2026-01", "2026-02");
        result.PorMes.Single(x => x.Etiqueta == "2026-01").Cantidad.Should().Be(3);
        result.PorMes.Single(x => x.Etiqueta == "2026-02").Cantidad.Should().Be(2);
    }
}
