using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Especialidades;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class EspecialidadService : IEspecialidadService
{
    private readonly VitalisDbContext _context;

    public EspecialidadService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task<List<EspecialidadDto>> ObtenerTodosAsync(string? buscar = null)
    {
        var query = _context.Especialidades.AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var b = buscar.Trim().ToLower();
            query = query.Where(e => e.Nombre.ToLower().Contains(b) || (e.Descripcion != null && e.Descripcion.ToLower().Contains(b)));
        }

        return await query
            .Select(e => new EspecialidadDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Descripcion = e.Descripcion
            })
            .ToListAsync();
    }

    public async Task<EspecialidadDto?> ObtenerPorIdAsync(int id)
    {
        var e = await _context.Especialidades.FindAsync(id);
        if (e == null) return null;

        return new EspecialidadDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Descripcion = e.Descripcion
        };
    }

    public async Task<EspecialidadDto> CrearAsync(CrearEspecialidadDto dto)
    {
        var e = new Especialidad
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion
        };

        _context.Especialidades.Add(e);
        await _context.SaveChangesAsync();

        return new EspecialidadDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Descripcion = e.Descripcion
        };
    }

    public async Task<EspecialidadDto?> EditarAsync(int id, EditarEspecialidadDto dto)
    {
        var e = await _context.Especialidades.FindAsync(id);
        if (e == null) return null;

        e.Nombre = dto.Nombre;
        e.Descripcion = dto.Descripcion;

        await _context.SaveChangesAsync();

        return new EspecialidadDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Descripcion = e.Descripcion
        };
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var e = await _context.Especialidades.FindAsync(id);
        if (e == null) return false;

        _context.Especialidades.Remove(e);
        await _context.SaveChangesAsync();
        return true;
    }
}
