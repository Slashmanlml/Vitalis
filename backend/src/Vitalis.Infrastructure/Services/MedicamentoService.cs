using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Medicamentos;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class MedicamentoService : IMedicamentoService
{
    private readonly VitalisDbContext _context;

    public MedicamentoService(VitalisDbContext context) => _context = context;

    public async Task<List<MedicamentoDto>> ObtenerTodosAsync(string? buscar = null)
    {
        var query = _context.Medicamentos.Where(m => m.Activo).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var b = buscar.ToLower();
            query = query.Where(m => m.Nombre.ToLower().Contains(b));
        }
        return await query
            .Select(m => new MedicamentoDto
            {
                Id = m.Id, Nombre = m.Nombre, Presentacion = m.Presentacion, Activo = m.Activo
            })
            .ToListAsync();
    }

    public async Task<MedicamentoDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Medicamentos
            .Where(m => m.Id == id)
            .Select(m => new MedicamentoDto { Id = m.Id, Nombre = m.Nombre, Presentacion = m.Presentacion, Activo = m.Activo })
            .FirstOrDefaultAsync();
    }

    public async Task<MedicamentoDto> CrearAsync(CrearMedicamentoDto dto)
    {
        var med = new Medicamento { Nombre = dto.Nombre, Presentacion = dto.Presentacion, Activo = true };
        _context.Medicamentos.Add(med);
        await _context.SaveChangesAsync();
        return new MedicamentoDto { Id = med.Id, Nombre = med.Nombre, Presentacion = med.Presentacion, Activo = med.Activo };
    }

    public async Task<MedicamentoDto?> EditarAsync(int id, EditarMedicamentoDto dto)
    {
        var med = await _context.Medicamentos.FindAsync(id);
        if (med == null) return null;
        med.Nombre = dto.Nombre;
        med.Presentacion = dto.Presentacion;
        med.Activo = dto.Activo;
        await _context.SaveChangesAsync();
        return await ObtenerPorIdAsync(id);
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var med = await _context.Medicamentos.FindAsync(id);
        if (med == null) return false;
        med.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
