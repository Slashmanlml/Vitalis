using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Portal;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class PortalPacienteService : IPortalPacienteService
{
    private readonly VitalisDbContext _context;
    private readonly IUsuarioActual _usuarioActual;

    public PortalPacienteService(VitalisDbContext context, IUsuarioActual usuarioActual)
    {
        _context = context;
        _usuarioActual = usuarioActual;
    }

    public async Task<MiPerfilPacienteDto> ObtenerMiPerfilAsync(CancellationToken ct = default)
    {
        var pacienteId = await ResolverMiPacienteIdAsync(ct);

        return await _context.Pacientes
            .AsNoTracking()
            .Where(p => p.Id == pacienteId)
            .Select(p => new MiPerfilPacienteDto
            {
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Dni = p.Dni,
                FechaNacimiento = p.FechaNacimiento,
                Email = p.Email,
                Telefono = p.Telefono,
                ObraSocialNombre = p.ObraSocial != null ? p.ObraSocial.Nombre : null,
                NumeroAfiliado = p.NumeroAfiliado
            })
            .FirstAsync(ct);
    }

    public async Task<IEnumerable<MiTurnoDto>> ObtenerMisTurnosAsync(CancellationToken ct = default)
    {
        var pacienteId = await ResolverMiPacienteIdAsync(ct);

        return await _context.Turnos
            .AsNoTracking()
            .Where(t => t.PacienteId == pacienteId)
            .OrderByDescending(t => t.FechaHora)
            .Select(t => new MiTurnoDto
            {
                Id = t.Id,
                FechaHora = t.FechaHora,
                ProfesionalNombre = t.Profesional!.Nombre + " " + t.Profesional!.Apellido,
                Especialidad = t.Profesional!.Especialidad != null ? t.Profesional!.Especialidad!.Nombre : "",
                Estado = t.Estado,
                Confirmado = t.Confirmado
            })
            .ToListAsync(ct);
    }

    /// <summary>Usuario del token → su ficha. Nunca se acepta un id del cliente.</summary>
    private async Task<int> ResolverMiPacienteIdAsync(CancellationToken ct)
    {
        var usuarioId = _usuarioActual.UsuarioId
            ?? throw new ForbiddenException("No hay una sesión válida.");

        var pacienteId = await _context.Pacientes
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId && p.Activo)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        return pacienteId ?? throw new NotFoundException("Tu cuenta no tiene una ficha de paciente activa vinculada.");
    }
}
