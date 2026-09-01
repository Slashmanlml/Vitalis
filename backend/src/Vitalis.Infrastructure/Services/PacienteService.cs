using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Pacientes;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class PacienteService : IPacienteService
{
    private readonly VitalisDbContext _context;

    public PacienteService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task<List<PacienteDto>> ObtenerTodosAsync(string? buscar = null, bool incluirInactivos = false)
    {
        var query = _context.Pacientes
            .Include(p => p.ObraSocial)
            .AsQueryable();

        // El listado muestra solo los activos salvo que se pidan todos. La baja es
        // lógica porque la historia clínica del paciente debe conservarse, pero eso
        // exige una manera de volver a verlo: de otro modo, un paciente dado de baja
        // por error desaparece para siempre de la interfaz.
        if (!incluirInactivos)
        {
            query = query.Where(p => p.Activo);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var b = buscar.Trim().ToLower();
            query = query.Where(p =>
                p.Nombre.ToLower().Contains(b) ||
                p.Apellido.ToLower().Contains(b) ||
                p.Dni.Contains(b));
        }

        return await query
            .Select(p => new PacienteDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Dni = p.Dni,
                FechaNacimiento = p.FechaNacimiento,
                Telefono = p.Telefono,
                Email = p.Email,
                Direccion = p.Direccion,
                ObraSocialId = p.ObraSocialId,
                ObraSocialNombre = p.ObraSocial != null ? p.ObraSocial.Nombre : null,
                NumeroAfiliado = p.NumeroAfiliado,
                FotoUrl = p.FotoUrl,
                Activo = p.Activo
            })
            .ToListAsync();
    }

    public async Task<PacienteDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Pacientes
            .Include(p => p.ObraSocial)
            .Where(p => p.Id == id)
            .Select(p => new PacienteDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Dni = p.Dni,
                FechaNacimiento = p.FechaNacimiento,
                Telefono = p.Telefono,
                Email = p.Email,
                Direccion = p.Direccion,
                ObraSocialId = p.ObraSocialId,
                ObraSocialNombre = p.ObraSocial != null ? p.ObraSocial.Nombre : null,
                NumeroAfiliado = p.NumeroAfiliado,
                FotoUrl = p.FotoUrl,
                Activo = p.Activo
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PacienteDto> CrearAsync(CrearPacienteDto dto)
    {
        var existeDni = await _context.Pacientes.AnyAsync(p => p.Dni == dto.Dni);
        if (existeDni)
        {
            throw new ConflictException("Ya existe un paciente registrado con el DNI ingresado.");
        }

        var paciente = new Paciente
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Dni = dto.Dni,
            // PostgreSQL exige DateTimeKind.Utc para timestamptz; el JSON del cliente
            // suele llegar como fecha "pelada" (Kind=Unspecified) y Npgsql la rechaza.
            FechaNacimiento = DateTime.SpecifyKind(dto.FechaNacimiento, DateTimeKind.Utc),
            Telefono = dto.Telefono,
            Email = dto.Email,
            Direccion = dto.Direccion,
            ObraSocialId = dto.ObraSocialId,
            NumeroAfiliado = dto.NumeroAfiliado,
            FotoUrl = dto.FotoUrl,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(paciente.Id) ?? throw new Exception("Error al crear paciente");
    }

    public async Task<PacienteDto?> EditarAsync(int id, EditarPacienteDto dto)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null) return null;

        paciente.Nombre = dto.Nombre;
        paciente.Apellido = dto.Apellido;
        paciente.Telefono = dto.Telefono;
        paciente.Email = dto.Email;
        paciente.Direccion = dto.Direccion;
        paciente.ObraSocialId = dto.ObraSocialId;
        paciente.NumeroAfiliado = dto.NumeroAfiliado;
        paciente.FotoUrl = dto.FotoUrl ?? paciente.FotoUrl;

        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(id);
    }

    public async Task<bool> ReactivarAsync(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null) return false;

        paciente.Activo = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null) return false;

        paciente.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}