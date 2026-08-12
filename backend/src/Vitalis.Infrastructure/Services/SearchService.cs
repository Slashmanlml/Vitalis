using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly VitalisDbContext _context;

    public SearchService(VitalisDbContext context) => _context = context;

    public async Task<SearchResultDto> BuscarAsync(string query)
    {
        var lower = query.ToLower();

        var pacientes = await _context.Pacientes
            .Where(p => p.Nombre.ToLower().Contains(lower) || p.Apellido.ToLower().Contains(lower) || p.Dni.Contains(lower))
            .Take(5)
            .Select(p => new SearchItemDto
            {
                Id = p.Id, Tipo = "Paciente",
                Titulo = p.Nombre + " " + p.Apellido,
                Subtitulo = "DNI: " + p.Dni,
                Ruta = "/dashboard/pacientes"
            })
            .ToListAsync();

        var profesionales = await _context.Profesionales
            .Where(p => p.Nombre.ToLower().Contains(lower) || p.Apellido.ToLower().Contains(lower) || p.Matricula.Contains(lower))
            .Take(5)
            .Select(p => new SearchItemDto
            {
                Id = p.Id, Tipo = "Profesional",
                Titulo = p.Nombre + " " + p.Apellido,
                Subtitulo = "Matrícula: " + p.Matricula,
                Ruta = "/dashboard/profesionales"
            })
            .ToListAsync();

        var turnos = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional)
            .Where(t => t.Paciente.Nombre.ToLower().Contains(lower) || t.Paciente.Apellido.ToLower().Contains(lower)
                || t.Profesional.Nombre.ToLower().Contains(lower) || t.Profesional.Apellido.ToLower().Contains(lower))
            .Take(5)
            .Select(t => new SearchItemDto
            {
                Id = t.Id, Tipo = "Turno",
                Titulo = t.Paciente.Nombre + " " + t.Paciente.Apellido,
                Subtitulo = t.Profesional.Nombre + " " + t.Profesional.Apellido + " - " + t.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                Ruta = "/dashboard/turnos"
            })
            .ToListAsync();

        return new SearchResultDto
        {
            Pacientes = pacientes,
            Profesionales = profesionales,
            Turnos = turnos
        };
    }
}
