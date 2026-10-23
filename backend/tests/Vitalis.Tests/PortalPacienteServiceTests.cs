using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitalis.Domain.Constants;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;
using Xunit;

namespace Vitalis.Tests;

public class PortalPacienteServiceTests
{
    private readonly VitalisDbContext _context;
    private readonly UsuarioActualDePrueba _usuarioActual = new() { UsuarioId = 10, Rol = Roles.Paciente };
    private readonly PortalPacienteService _service;

    public PortalPacienteServiceTests()
    {
        var options = new DbContextOptionsBuilder<VitalisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new VitalisDbContext(options, new Microsoft.AspNetCore.Http.HttpContextAccessor());

        _context.Roles.Add(new Rol { Id = 5, Nombre = Roles.Paciente });
        _context.Usuarios.AddRange(
            new Usuario { Id = 10, Nombre = "Juana", Apellido = "Pérez", Email = "juana@example.com", PasswordHash = "x", RolId = 5 },
            new Usuario { Id = 11, Nombre = "Otro", Apellido = "Paciente", Email = "otro@example.com", PasswordHash = "x", RolId = 5 });
        var esp = new Especialidad { Id = 1, Nombre = "Cardiología" };
        _context.Especialidades.Add(esp);
        _context.Profesionales.Add(new Profesional { Id = 1, Nombre = "Laura", Apellido = "Martínez", Matricula = "MP-1", EspecialidadId = 1 });
        _context.ObrasSociales.Add(new ObraSocial { Id = 1, Nombre = "OSDE", Codigo = "OSDE" });
        _context.Pacientes.AddRange(
            new Paciente { Id = 100, Nombre = "Juana", Apellido = "Pérez", Dni = "1", UsuarioId = 10, Activo = true },
            new Paciente { Id = 101, Nombre = "Otro", Apellido = "Paciente", Dni = "2", UsuarioId = 11, Activo = true });
        _context.Turnos.AddRange(
            new Turno { Id = 1, PacienteId = 100, ProfesionalId = 1, ObraSocialId = 1, FechaHora = DateTime.UtcNow.AddDays(2), Estado = "Confirmado" },
            new Turno { Id = 2, PacienteId = 100, ProfesionalId = 1, ObraSocialId = 1, FechaHora = DateTime.UtcNow.AddDays(-5), Estado = "Atendido" },
            new Turno { Id = 3, PacienteId = 101, ProfesionalId = 1, ObraSocialId = 1, FechaHora = DateTime.UtcNow.AddDays(1), Estado = "Solicitado" });
        _context.SaveChanges();

        _service = new PortalPacienteService(_context, _usuarioActual);
    }

    [Fact]
    public async Task Solo_devuelve_los_turnos_de_la_ficha_del_token()
    {
        var turnos = (await _service.ObtenerMisTurnosAsync()).ToList();

        turnos.Select(t => t.Id).Should().BeEquivalentTo(new[] { 1, 2 });
        turnos.First().Id.Should().Be(1, "el más reciente primero");
        turnos.First().Especialidad.Should().Be("Cardiología");
    }

    [Fact]
    public async Task Otro_usuario_ve_solo_lo_suyo()
    {
        _usuarioActual.UsuarioId = 11;
        var turnos = (await _service.ObtenerMisTurnosAsync()).ToList();
        turnos.Select(t => t.Id).Should().BeEquivalentTo(new[] { 3 });
    }

    [Fact]
    public async Task Mi_perfil_devuelve_la_ficha_vinculada()
    {
        var perfil = await _service.ObtenerMiPerfilAsync();
        perfil.Nombre.Should().Be("Juana");
        perfil.Dni.Should().Be("1");
    }

    [Fact]
    public async Task Sin_ficha_vinculada_responde_no_encontrado()
    {
        _usuarioActual.UsuarioId = 999;
        var pedir = () => _service.ObtenerMisTurnosAsync();
        await pedir.Should().ThrowAsync<NotFoundException>();
    }
}
