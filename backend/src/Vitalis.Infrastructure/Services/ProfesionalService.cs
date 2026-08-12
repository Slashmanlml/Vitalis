using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Profesionales;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;

namespace Vitalis.Infrastructure.Services;

public class ProfesionalService : IProfesionalService
{
    private readonly VitalisDbContext _context;

    public ProfesionalService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProfesionalDto>> ObtenerTodosAsync()
    {
        return await _context.Profesionales
            .Include(p => p.Especialidad)
            .Select(p => new ProfesionalDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Matricula = p.Matricula,
                Email = p.Email ?? string.Empty,
                EspecialidadId = p.EspecialidadId,
                EspecialidadNombre = p.Especialidad != null ? p.Especialidad.Nombre : string.Empty,
                FotoUrl = p.FotoUrl,
                Activo = p.Activo
            })
            .ToListAsync();
    }

    public async Task<ProfesionalDto?> ObtenerPorIdAsync(int id)
    {
        var profesional = await _context.Profesionales
            .Include(p => p.Especialidad)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profesional == null) return null;

        return new ProfesionalDto
        {
            Id = profesional.Id,
            Nombre = profesional.Nombre,
            Apellido = profesional.Apellido,
            Matricula = profesional.Matricula,
            Email = profesional.Email ?? string.Empty,
            EspecialidadId = profesional.EspecialidadId,
            EspecialidadNombre = profesional.Especialidad != null ? profesional.Especialidad.Nombre : string.Empty,
            FotoUrl = profesional.FotoUrl,
            Activo = profesional.Activo
        };
    }

    public async Task<ProfesionalDto> CrearAsync(CrearProfesionalDto dto)
    {
        var existeMatricula = await _context.Profesionales.AnyAsync(p => p.Matricula == dto.Matricula);
        if (existeMatricula)
        {
            throw new ConflictException("Ya existe un profesional registrado con la matrícula ingresada.");
        }

        var profesional = new Profesional
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Matricula = dto.Matricula,
            Email = dto.Email,
            EspecialidadId = dto.EspecialidadId,
            FotoUrl = dto.FotoUrl,
            Activo = true
        };

        _context.Profesionales.Add(profesional);
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(profesional.Id) ?? throw new Exception("Error al crear profesional");
    }

    public async Task<ProfesionalDto?> EditarAsync(int id, EditarProfesionalDto dto)
    {
        var profesional = await _context.Profesionales.FindAsync(id);
        if (profesional == null) return null;

        if (dto.Matricula != null && dto.Matricula != profesional.Matricula)
        {
            var existeMatricula = await _context.Profesionales.AnyAsync(p => p.Matricula == dto.Matricula && p.Id != id);
            if (existeMatricula)
            {
                throw new ConflictException("Ya existe un profesional registrado con la matrícula ingresada.");
            }
        }

        profesional.Nombre = dto.Nombre ?? profesional.Nombre;
        profesional.Apellido = dto.Apellido ?? profesional.Apellido;
        profesional.Matricula = dto.Matricula ?? profesional.Matricula;
        profesional.Email = dto.Email ?? profesional.Email;
        profesional.EspecialidadId = dto.EspecialidadId ?? profesional.EspecialidadId;
        profesional.FotoUrl = dto.FotoUrl ?? profesional.FotoUrl;
        profesional.Activo = dto.Activo ?? profesional.Activo;

        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(profesional.Id);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var profesional = await _context.Profesionales.FindAsync(id);
        if (profesional == null) return false;

        _context.Profesionales.Remove(profesional);
        await _context.SaveChangesAsync();
        return true;
    }
}
