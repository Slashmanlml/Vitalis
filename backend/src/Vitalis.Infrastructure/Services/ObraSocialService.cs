using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.ObrasSociales;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;
using Vitalis.Domain.Entities;

namespace Vitalis.Infrastructure.Services;

public class ObraSocialService : IObraSocialService
{
    private readonly VitalisDbContext _context;

    public ObraSocialService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ObraSocialDto>> ObtenerTodasAsync()
    {
        return await _context.ObrasSociales
            .Select(o => new ObraSocialDto
            {
                Id = o.Id,
                Nombre = o.Nombre,
                Activa = o.Activa
            })
            .ToListAsync();
    }

    public async Task<ObraSocialDto?> ObtenerPorIdAsync(int id)
    {
        var obra = await _context.ObrasSociales.FindAsync(id);
        if (obra == null) return null;

        return new ObraSocialDto
        {
            Id = obra.Id,
            Nombre = obra.Nombre,
            Activa = obra.Activa
        };
    }

    public async Task<ObraSocialDto> CrearAsync(CrearObraSocialDto dto)
    {
        var obra = new ObraSocial
        {
            Nombre = dto.Nombre,
            Activa = true
        };

        _context.ObrasSociales.Add(obra);
        await _context.SaveChangesAsync();

        return new ObraSocialDto
        {
            Id = obra.Id,
            Nombre = obra.Nombre,
            Activa = obra.Activa
        };
    }

    public async Task<ObraSocialDto?> EditarAsync(int id, EditarObraSocialDto dto)
    {
        var obra = await _context.ObrasSociales.FindAsync(id);
        if (obra == null) return null;

        obra.Nombre = dto.Nombre ?? obra.Nombre;
        obra.Activa = dto.Activa ?? obra.Activa;

        await _context.SaveChangesAsync();

        return new ObraSocialDto
        {
            Id = obra.Id,
            Nombre = obra.Nombre,
            Activa = obra.Activa
        };
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var obra = await _context.ObrasSociales.FindAsync(id);
        if (obra == null) return false;

        _context.ObrasSociales.Remove(obra);
        await _context.SaveChangesAsync();
        return true;
    }
}
