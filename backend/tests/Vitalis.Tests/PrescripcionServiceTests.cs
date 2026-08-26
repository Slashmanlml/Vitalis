using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Prescripciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class PrescripcionServiceTests
{
    private readonly IPrescripcionService _service;
    private readonly VitalisDbContext _context;

    public PrescripcionServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new PrescripcionService(_context, new NoOpEmailService());

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

        _context.Pacientes.AddRange(
            new Paciente { Id = 1, Nombre = "Juan", Apellido = "Perez", Dni = "12345678", FechaNacimiento = new DateTime(1990, 1, 1), ObraSocialId = 1, Activo = true, FechaCreacion = DateTime.UtcNow },
            new Paciente { Id = 2, Nombre = "Maria", Apellido = "Lopez", Dni = "87654321", FechaNacimiento = new DateTime(1985, 5, 5), ObraSocialId = 1, Activo = true, FechaCreacion = DateTime.UtcNow }
        );

        _context.Medicamentos.AddRange(
            new Medicamento { Id = 1, Nombre = "Ibuprofeno", Presentacion = "600mg comprimidos", Activo = true },
            new Medicamento { Id = 2, Nombre = "Amoxicilina", Presentacion = "500mg comprimidos", Activo = true }
        );

        // Turno + ConsultaMedica de referencia: CrearAsync ahora valida que la consulta de
        // origen (ConsultaMedicaId), el paciente y el profesional existan antes de crear la
        // prescripción (mismo hallazgo/fix que ConsultaMedicaService, ver task.md), así que
        // los tests que llaman a CrearAsync necesitan una ConsultaMedica real.
        _context.Turnos.Add(new Turno
        {
            Id = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            ObraSocialId = 1,
            FechaHora = DateTime.SpecifyKind(new DateTime(2026, 1, 15, 10, 0, 0), DateTimeKind.Utc),
            Estado = "Atendido",
            Confirmado = true
        });

        _context.ConsultasMedicas.Add(new ConsultaMedica
        {
            Id = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            TurnoId = 1,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = "Control de rutina"
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task CrearAsync_Should_Add_Prescripcion_Con_Un_Medicamento()
    {
        var dto = new CrearPrescripcionDto
        {
            ConsultaMedicaId = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            Observaciones = "Tomar con alimentos",
            Detalles =
            {
                new CrearPrescripcionDetalleDto { MedicamentoId = 1, Dosis = "1 comprimido", Frecuencia = "cada 8hs", Duracion = "5 dias" }
            }
        };

        var result = await _service.CrearAsync(dto);

        result.Should().NotBeNull();
        result.PacienteNombre.Should().Be("Juan Perez");
        result.ProfesionalNombre.Should().Be("Alejandro Gomez");
        result.Detalles.Should().ContainSingle();
        result.Detalles[0].MedicamentoNombre.Should().Be("Ibuprofeno");
        result.Detalles[0].Dosis.Should().Be("1 comprimido");
    }

    [Fact]
    public async Task CrearAsync_Should_Permitir_Varios_Medicamentos_En_La_Misma_Prescripcion()
    {
        // Regla de negocio documentada en docs/02 y docs/04: una prescripción puede incluir
        // varios medicamentos, cada uno con su propia dosis/frecuencia/duración.
        var dto = new CrearPrescripcionDto
        {
            ConsultaMedicaId = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            Detalles =
            {
                new CrearPrescripcionDetalleDto { MedicamentoId = 1, Dosis = "1 comprimido", Frecuencia = "cada 8hs", Duracion = "5 dias" },
                new CrearPrescripcionDetalleDto { MedicamentoId = 2, Dosis = "1 comprimido", Frecuencia = "cada 12hs", Duracion = "7 dias" }
            }
        };

        var result = await _service.CrearAsync(dto);

        result.Detalles.Should().HaveCount(2);
        result.Detalles.Select(d => d.MedicamentoNombre).Should().BeEquivalentTo(new[] { "Ibuprofeno", "Amoxicilina" });
    }

    [Fact]
    public async Task ObtenerPorPacienteAsync_Should_Ordenar_Por_Fecha_Descendente()
    {
        _context.Prescripciones.AddRange(
            new Prescripcion { Id = 1, ConsultaMedicaId = 1, PacienteId = 1, ProfesionalId = 1, Fecha = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc), Observaciones = "Más antigua" },
            new Prescripcion { Id = 2, ConsultaMedicaId = 1, PacienteId = 1, ProfesionalId = 1, Fecha = new DateTime(2026, 1, 20, 9, 0, 0, DateTimeKind.Utc), Observaciones = "Más reciente" }
        );
        await _context.SaveChangesAsync();

        var result = await _service.ObtenerPorPacienteAsync(1);

        result.Should().HaveCount(2);
        result.First().Observaciones.Should().Be("Más reciente");
        result.Last().Observaciones.Should().Be("Más antigua");
    }

    [Fact]
    public async Task ObtenerPorPacienteAsync_Should_Filtrar_Por_Paciente()
    {
        _context.Prescripciones.AddRange(
            new Prescripcion { Id = 1, ConsultaMedicaId = 1, PacienteId = 1, ProfesionalId = 1, Fecha = DateTime.UtcNow },
            new Prescripcion { Id = 2, ConsultaMedicaId = 1, PacienteId = 2, ProfesionalId = 1, Fecha = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var result = await _service.ObtenerPorPacienteAsync(1);

        result.Should().ContainSingle();
        result.Single().PacienteId.Should().Be(1);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_Should_Return_Null_Cuando_No_Existe()
    {
        var result = await _service.ObtenerPorIdAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_Cuando_La_Consulta_No_Existe()
    {
        var dto = new CrearPrescripcionDto
        {
            ConsultaMedicaId = 9999, // no existe
            PacienteId = 1,
            ProfesionalId = 1,
            Detalles = { new CrearPrescripcionDetalleDto { MedicamentoId = 1, Dosis = "1 comprimido", Frecuencia = "cada 8hs", Duracion = "5 dias" } }
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Consulta médica no encontrada.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_Cuando_El_Paciente_No_Existe()
    {
        var dto = new CrearPrescripcionDto
        {
            ConsultaMedicaId = 1,
            PacienteId = 9999, // no existe
            ProfesionalId = 1,
            Detalles = { new CrearPrescripcionDetalleDto { MedicamentoId = 1, Dosis = "1 comprimido", Frecuencia = "cada 8hs", Duracion = "5 dias" } }
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Paciente no encontrado.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_Cuando_El_Profesional_No_Existe()
    {
        var dto = new CrearPrescripcionDto
        {
            ConsultaMedicaId = 1,
            PacienteId = 1,
            ProfesionalId = 9999, // no existe
            Detalles = { new CrearPrescripcionDetalleDto { MedicamentoId = 1, Dosis = "1 comprimido", Frecuencia = "cada 8hs", Duracion = "5 dias" } }
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Profesional no encontrado.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_Cuando_El_Medicamento_No_Existe()
    {
        var dto = new CrearPrescripcionDto
        {
            ConsultaMedicaId = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            Detalles = { new CrearPrescripcionDetalleDto { MedicamentoId = 9999, Dosis = "1 comprimido", Frecuencia = "cada 8hs", Duracion = "5 dias" } }
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Medicamento con id 9999 no encontrado.");
    }
}
