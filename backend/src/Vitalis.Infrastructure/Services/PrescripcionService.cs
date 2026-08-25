using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Prescripciones;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Domain.Exceptions;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class PrescripcionService : IPrescripcionService
{
    private readonly VitalisDbContext _context;

    public PrescripcionService(VitalisDbContext context) => _context = context;

    public async Task<List<PrescripcionDto>> ObtenerPorPacienteAsync(int pacienteId)
    {
        return await _context.Prescripciones
            .Include(p => p.Paciente)
            .Include(p => p.Profesional)
            .Include(p => p.Detalles).ThenInclude(d => d.Medicamento)
            .Where(p => p.PacienteId == pacienteId)
            .OrderByDescending(p => p.Fecha)
            .Select(p => new PrescripcionDto
            {
                Id = p.Id,
                ConsultaMedicaId = p.ConsultaMedicaId,
                PacienteId = p.PacienteId,
                PacienteNombre = p.Paciente.Nombre + " " + p.Paciente.Apellido,
                ProfesionalId = p.ProfesionalId,
                ProfesionalNombre = p.Profesional.Nombre + " " + p.Profesional.Apellido,
                Fecha = p.Fecha,
                Observaciones = p.Observaciones,
                Detalles = p.Detalles.Select(d => new PrescripcionDetalleDto
                {
                    Id = d.Id,
                    MedicamentoId = d.MedicamentoId,
                    MedicamentoNombre = d.Medicamento.Nombre,
                    Dosis = d.Dosis,
                    Frecuencia = d.Frecuencia,
                    Duracion = d.Duracion,
                    Indicaciones = d.Indicaciones
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<PrescripcionDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Prescripciones
            .Include(p => p.Paciente)
            .Include(p => p.Profesional)
            .Include(p => p.Detalles).ThenInclude(d => d.Medicamento)
            .Where(p => p.Id == id)
            .Select(p => new PrescripcionDto
            {
                Id = p.Id,
                ConsultaMedicaId = p.ConsultaMedicaId,
                PacienteId = p.PacienteId,
                PacienteNombre = p.Paciente.Nombre + " " + p.Paciente.Apellido,
                ProfesionalId = p.ProfesionalId,
                ProfesionalNombre = p.Profesional.Nombre + " " + p.Profesional.Apellido,
                Fecha = p.Fecha,
                Observaciones = p.Observaciones,
                Detalles = p.Detalles.Select(d => new PrescripcionDetalleDto
                {
                    Id = d.Id,
                    MedicamentoId = d.MedicamentoId,
                    MedicamentoNombre = d.Medicamento.Nombre,
                    Dosis = d.Dosis,
                    Frecuencia = d.Frecuencia,
                    Duracion = d.Duracion,
                    Indicaciones = d.Indicaciones
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PrescripcionDto> CrearAsync(CrearPrescripcionDto dto)
    {
        // Mismo hallazgo que en ConsultaMedicaService: las FKs de Prescripcion son
        // requeridas (navegaciones no-nulas) pero antes no se validaba su existencia.
        var consultaExiste = await _context.ConsultasMedicas.AnyAsync(c => c.Id == dto.ConsultaMedicaId);
        if (!consultaExiste)
        {
            throw new NotFoundException("Consulta médica no encontrada.");
        }

        var pacienteExiste = await _context.Pacientes.AnyAsync(p => p.Id == dto.PacienteId);
        if (!pacienteExiste)
        {
            throw new NotFoundException("Paciente no encontrado.");
        }

        var profesionalExiste = await _context.Profesionales.AnyAsync(p => p.Id == dto.ProfesionalId);
        if (!profesionalExiste)
        {
            throw new NotFoundException("Profesional no encontrado.");
        }

        foreach (var detalle in dto.Detalles)
        {
            var medicamentoExiste = await _context.Medicamentos.AnyAsync(m => m.Id == detalle.MedicamentoId);
            if (!medicamentoExiste)
            {
                throw new NotFoundException($"Medicamento con id {detalle.MedicamentoId} no encontrado.");
            }
        }

        var presc = new Prescripcion
        {
            ConsultaMedicaId = dto.ConsultaMedicaId,
            PacienteId = dto.PacienteId,
            ProfesionalId = dto.ProfesionalId,
            Fecha = DateTime.UtcNow,
            Observaciones = dto.Observaciones,
            Detalles = dto.Detalles.Select(d => new PrescripcionDetalle
            {
                MedicamentoId = d.MedicamentoId,
                Dosis = d.Dosis,
                Frecuencia = d.Frecuencia,
                Duracion = d.Duracion,
                Indicaciones = d.Indicaciones
            }).ToList()
        };

        _context.Prescripciones.Add(presc);
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(presc.Id) ?? throw new Exception("Error al crear prescripcion");
    }
}
