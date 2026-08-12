using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Prestaciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class PrestacionService : IPrestacionService
{
    private readonly VitalisDbContext _context;

    public PrestacionService(VitalisDbContext context) => _context = context;

    public async Task<List<PrestacionDto>> ObtenerTodasAsync()
    {
        return await _context.Prestaciones
            .Select(p => new PrestacionDto
            {
                Id = p.Id, Nombre = p.Nombre, Codigo = p.Codigo,
                ImporteBase = p.ImporteBase, Activa = p.Activa
            })
            .ToListAsync();
    }

    public async Task<PrestacionDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Prestaciones
            .Where(p => p.Id == id)
            .Select(p => new PrestacionDto
            {
                Id = p.Id, Nombre = p.Nombre, Codigo = p.Codigo,
                ImporteBase = p.ImporteBase, Activa = p.Activa
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PrestacionDto> CrearAsync(CrearPrestacionDto dto)
    {
        var ent = new Prestacion { Nombre = dto.Nombre, Codigo = dto.Codigo, ImporteBase = dto.ImporteBase, Activa = true };
        _context.Prestaciones.Add(ent);
        await _context.SaveChangesAsync();
        return new PrestacionDto { Id = ent.Id, Nombre = ent.Nombre, Codigo = ent.Codigo, ImporteBase = ent.ImporteBase, Activa = ent.Activa };
    }

    public async Task<PrestacionDto?> EditarAsync(int id, EditarPrestacionDto dto)
    {
        var ent = await _context.Prestaciones.FindAsync(id);
        if (ent == null) return null;
        ent.Nombre = dto.Nombre;
        ent.Codigo = dto.Codigo;
        ent.ImporteBase = dto.ImporteBase;
        ent.Activa = dto.Activa;
        await _context.SaveChangesAsync();
        return await ObtenerPorIdAsync(id);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var ent = await _context.Prestaciones.FindAsync(id);
        if (ent == null) return false;
        ent.Activa = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
