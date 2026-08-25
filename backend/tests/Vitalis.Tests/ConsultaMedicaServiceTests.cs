using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Consultas;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class ConsultaMedicaServiceTests
{
    private readonly IConsultaMedicaService _service;
    private readonly VitalisDbContext _context;

    public ConsultaMedicaServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new HttpContextAccessor());
        _service = new ConsultaMedicaService(_context);

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
            new Paciente
            {
                Id = 1,
                Nombre = "Juan",
                Apellido = "Perez",
                Dni = "12345678",
                FechaNacimiento = new DateTime(1990, 1, 1),
                ObraSocialId = 1,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            },
            new Paciente
            {
                Id = 2,
                Nombre = "Maria",
                Apellido = "Lopez",
                Dni = "87654321",
                FechaNacimiento = new DateTime(1985, 5, 5),
                ObraSocialId = 1,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            }
        );

        _context.Turnos.Add(NuevoTurno(id: 1, pacienteId: 1, profesionalId: 1, estado: "Confirmado"));

        _context.SaveChanges();
    }

    private static Turno NuevoTurno(int id, int pacienteId, int profesionalId, string estado)
    {
        return new Turno
        {
            Id = id,
            PacienteId = pacienteId,
            ProfesionalId = profesionalId,
            ObraSocialId = 1,
            FechaHora = DateTime.SpecifyKind(new DateTime(2026, 1, 15, 10, 0, 0), DateTimeKind.Utc),
            Estado = estado,
            Confirmado = true
        };
    }

    [Fact]
    public async Task CrearAsync_Should_Add_Consulta_Con_Nombres_De_Paciente_Y_Profesional()
    {
        var dto = new CrearConsultaDto
        {
            TurnoId = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            MotivoConsulta = "Control de rutina",
            Diagnostico = "Sin hallazgos",
        };

        var result = await _service.CrearAsync(dto);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.PacienteNombre.Should().Be("Juan Perez");
        result.ProfesionalNombre.Should().Be("Alejandro Gomez");
        result.MotivoConsulta.Should().Be("Control de rutina");
        result.Diagnostico.Should().Be("Sin hallazgos");
    }

    [Fact]
    public async Task CrearAsync_Should_Marcar_El_Turno_De_Origen_Como_Atendido()
    {
        var dto = new CrearConsultaDto
        {
            TurnoId = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            MotivoConsulta = "Control de rutina"
        };

        await _service.CrearAsync(dto);

        var turno = await _context.Turnos.FindAsync(1);
        turno!.Estado.Should().Be("Atendido");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_Cuando_El_Turno_No_Existe()
    {
        // Antes este servicio no validaba que TurnoId/PacienteId/ProfesionalId
        // correspondieran a registros reales, lo que permitía crear una consulta
        // "huérfana" (hallazgo documentado en task.md, semana 3). Se corrigió para que
        // valide su existencia igual que BloqueoAgendaService, lanzando NotFoundException.
        var dto = new CrearConsultaDto
        {
            TurnoId = 9999, // no existe
            PacienteId = 1,
            ProfesionalId = 1,
            MotivoConsulta = "Consulta sin turno de origen"
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Turno no encontrado.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_Cuando_El_Paciente_No_Existe()
    {
        var dto = new CrearConsultaDto
        {
            TurnoId = 1,
            PacienteId = 9999, // no existe
            ProfesionalId = 1,
            MotivoConsulta = "Consulta con paciente inexistente"
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Paciente no encontrado.");
    }

    [Fact]
    public async Task CrearAsync_Should_Throw_NotFoundException_Cuando_El_Profesional_No_Existe()
    {
        var dto = new CrearConsultaDto
        {
            TurnoId = 1,
            PacienteId = 1,
            ProfesionalId = 9999, // no existe
            MotivoConsulta = "Consulta con profesional inexistente"
        };

        var act = async () => await _service.CrearAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Profesional no encontrado.");
    }

    [Fact]
    public async Task ObtenerPorPacienteAsync_Should_Ordenar_Por_Fecha_Descendente()
    {
        // Turno.ConsultaMedica es una relación uno a uno (no una lista): cada turno admite
        // como máximo una consulta asociada. Por eso cada ConsultaMedica de este test usa
        // un TurnoId distinto — reusar el mismo TurnoId en dos consultas hace que EF Core
        // rompa la asociación anterior y falle ("severed relationship") al ser una FK
        // requerida.
        _context.Turnos.Add(NuevoTurno(id: 2, pacienteId: 1, profesionalId: 1, estado: "Confirmado"));

        _context.ConsultasMedicas.AddRange(
            new ConsultaMedica
            {
                Id = 1,
                PacienteId = 1,
                ProfesionalId = 1,
                TurnoId = 1,
                Fecha = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                MotivoConsulta = "Consulta más antigua"
            },
            new ConsultaMedica
            {
                Id = 2,
                PacienteId = 1,
                ProfesionalId = 1,
                TurnoId = 2,
                Fecha = new DateTime(2026, 1, 20, 9, 0, 0, DateTimeKind.Utc),
                MotivoConsulta = "Consulta más reciente"
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.ObtenerPorPacienteAsync(1);

        result.Should().HaveCount(2);
        result.First().MotivoConsulta.Should().Be("Consulta más reciente");
        result.Last().MotivoConsulta.Should().Be("Consulta más antigua");
    }

    [Fact]
    public async Task EditarAsync_Should_Actualizar_Campos_Clinicos()
    {
        var creada = await _service.CrearAsync(new CrearConsultaDto
        {
            TurnoId = 1,
            PacienteId = 1,
            ProfesionalId = 1,
            MotivoConsulta = "Motivo original"
        });

        var editada = await _service.EditarAsync(creada.Id, new EditarConsultaDto
        {
            MotivoConsulta = "Motivo actualizado",
            Diagnostico = "Diagnóstico actualizado",
            Evolucion = "Evolución favorable"
        });

        editada.Should().NotBeNull();
        editada!.MotivoConsulta.Should().Be("Motivo actualizado");
        editada.Diagnostico.Should().Be("Diagnóstico actualizado");
        editada.Evolucion.Should().Be("Evolución favorable");
    }

    [Fact]
    public async Task EditarAsync_Should_Return_Null_Cuando_El_Id_No_Existe()
    {
        var result = await _service.EditarAsync(9999, new EditarConsultaDto
        {
            MotivoConsulta = "No debería aplicarse"
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CrearAntecedenteAsync_Should_Registrar_Fecha_Y_Devolver_El_Antecedente()
    {
        var result = await _service.CrearAntecedenteAsync(new CrearAntecedenteDto
        {
            PacienteId = 1,
            Tipo = "Quirúrgico",
            Descripcion = "Apendicectomía (2015)"
        });

        result.Should().NotBeNull();
        result.PacienteId.Should().Be(1);
        result.Tipo.Should().Be("Quirúrgico");
        result.FechaRegistro.Should().NotBe(default);
    }

    [Fact]
    public async Task ObtenerAntecedentesAsync_Should_Filtrar_Por_Paciente()
    {
        await _service.CrearAntecedenteAsync(new CrearAntecedenteDto { PacienteId = 1, Tipo = "Alergia alimentaria", Descripcion = "Maní" });
        await _service.CrearAntecedenteAsync(new CrearAntecedenteDto { PacienteId = 2, Tipo = "Quirúrgico", Descripcion = "Apendicectomía" });

        var result = await _service.ObtenerAntecedentesAsync(1);

        result.Should().ContainSingle();
        result.Single().PacienteId.Should().Be(1);
    }

    [Fact]
    public async Task CrearAlergiaAsync_Should_Crear_Alergia_Activa_Por_Defecto()
    {
        var result = await _service.CrearAlergiaAsync(new CrearAlergiaDto
        {
            PacienteId = 1,
            Sustancia = "Penicilina",
            Reaccion = "Urticaria",
            Severidad = "Alta"
        });

        result.Should().NotBeNull();
        result.Sustancia.Should().Be("Penicilina");
        result.Activa.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerAlergiasAsync_Should_Excluir_Alergias_Inactivas()
    {
        _context.Alergias.AddRange(
            new Alergia { Id = 1, PacienteId = 1, Sustancia = "Penicilina", Activa = true },
            new Alergia { Id = 2, PacienteId = 1, Sustancia = "Aspirina", Activa = false }
        );
        await _context.SaveChangesAsync();

        var result = await _service.ObtenerAlergiasAsync(1);

        result.Should().ContainSingle();
        result.Single().Sustancia.Should().Be("Penicilina");
    }
}
