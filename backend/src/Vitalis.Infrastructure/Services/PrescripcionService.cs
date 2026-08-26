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
    private readonly IEmailService? _emailService;

    public PrescripcionService(VitalisDbContext context, IEmailService? emailService = null)
    {
        _context = context;
        _emailService = emailService;
    }

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

        if (_emailService != null)
        {
            var pac = await _context.Pacientes.FindAsync(presc.PacienteId);
            var prof = await _context.Profesionales.FindAsync(presc.ProfesionalId);
            if (pac != null && prof != null)
            {
                var meds = await _context.Medicamentos
                    .Where(m => dto.Detalles.Select(d => d.MedicamentoId).Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, m => m.Nombre);

                string fechaStr = presc.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                string asunto = "Nueva Receta Médica Emitida - Vitalis";
                string medsHtml = string.Join("", dto.Detalles.Select(d => 
                    $"<li><strong>{(meds.ContainsKey(d.MedicamentoId) ? meds[d.MedicamentoId] : "Medicamento")}</strong>: {d.Dosis} - {d.Frecuencia} por {d.Duracion}</li>"));
                
                string cuerpo = $@"<div style='font-family: Arial, sans-serif; padding: 20px; color: #1e293b; background: #f8fafc; border-radius: 8px;'>
                    <div style='background: #0f766e; color: #fff; padding: 15px 20px; border-radius: 6px; text-align: center;'>
                        <h2 style='margin:0;'>Nueva Receta Médica Electrónica</h2>
                    </div>
                    <div style='padding: 20px; background: #fff; margin-top: 15px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                        <p>Estimado/a <strong>{pac.Nombre} {pac.Apellido}</strong>,</p>
                        <p>El profesional <strong>Dr/Dra. {prof.Nombre} {prof.Apellido}</strong> ha emitido una receta médica para usted el <strong>{fechaStr}</strong>.</p>
                        <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                        <p><strong>Medicamentos prescriptos:</strong></p>
                        <ul>{medsHtml}</ul>
                        {(string.IsNullOrWhiteSpace(presc.Observaciones) ? "" : $"<p><strong>Observaciones:</strong> {presc.Observaciones}</p>")}
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center; margin-top: 15px;'>Equipo Vitalis - Prescripciones Digitales</p>
                </div>";

                await _emailService.SendEmailAsync(pac.Email ?? "paciente@vitalis.local", asunto, cuerpo);
            }
        }

        return await ObtenerPorIdAsync(presc.Id) ?? throw new Exception("Error al crear prescripcion");
    }
}
