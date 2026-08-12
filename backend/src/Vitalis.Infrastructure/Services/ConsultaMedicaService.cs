using Microsoft.EntityFrameworkCore;
using Vitalis.Application.DTOs.Consultas;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class ConsultaMedicaService : IConsultaMedicaService
{
    private readonly VitalisDbContext _context;

    public ConsultaMedicaService(VitalisDbContext context) => _context = context;

    public async Task<List<ConsultaMedicaDto>> ObtenerPorPacienteAsync(int pacienteId)
    {
        return await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Profesional)
            .Where(c => c.PacienteId == pacienteId)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new ConsultaMedicaDto
            {
                Id = c.Id,
                PacienteId = c.PacienteId,
                PacienteNombre = c.Paciente.Nombre + " " + c.Paciente.Apellido,
                ProfesionalId = c.ProfesionalId,
                ProfesionalNombre = c.Profesional.Nombre + " " + c.Profesional.Apellido,
                TurnoId = c.TurnoId,
                Fecha = c.Fecha,
                MotivoConsulta = c.MotivoConsulta,
                Diagnostico = c.Diagnostico,
                Evolucion = c.Evolucion,
                Indicaciones = c.Indicaciones,
                Observaciones = c.Observaciones,
                EstudioAdjuntoUrl = c.EstudioAdjuntoUrl
            })
            .ToListAsync();
    }

    public async Task<ConsultaMedicaDto?> ObtenerPorIdAsync(int id)
    {
        return await _context.ConsultasMedicas
            .Include(c => c.Paciente)
            .Include(c => c.Profesional)
            .Where(c => c.Id == id)
            .Select(c => new ConsultaMedicaDto
            {
                Id = c.Id,
                PacienteId = c.PacienteId,
                PacienteNombre = c.Paciente.Nombre + " " + c.Paciente.Apellido,
                ProfesionalId = c.ProfesionalId,
                ProfesionalNombre = c.Profesional.Nombre + " " + c.Profesional.Apellido,
                TurnoId = c.TurnoId,
                Fecha = c.Fecha,
                MotivoConsulta = c.MotivoConsulta,
                Diagnostico = c.Diagnostico,
                Evolucion = c.Evolucion,
                Indicaciones = c.Indicaciones,
                Observaciones = c.Observaciones,
                EstudioAdjuntoUrl = c.EstudioAdjuntoUrl
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ConsultaMedicaDto> CrearAsync(CrearConsultaDto dto)
    {
        var consulta = new ConsultaMedica
        {
            PacienteId = dto.PacienteId,
            ProfesionalId = dto.ProfesionalId,
            TurnoId = dto.TurnoId,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = dto.MotivoConsulta,
            Diagnostico = dto.Diagnostico,
            Evolucion = dto.Evolucion,
            Indicaciones = dto.Indicaciones,
            Observaciones = dto.Observaciones,
            EstudioAdjuntoUrl = dto.EstudioAdjuntoUrl
        };

        var turno = await _context.Turnos.FindAsync(dto.TurnoId);
        if (turno != null) turno.Estado = "Atendido";

        _context.ConsultasMedicas.Add(consulta);
        await _context.SaveChangesAsync();

        return await ObtenerPorIdAsync(consulta.Id) ?? throw new Exception("Error al crear consulta");
    }

    public async Task<ConsultaMedicaDto?> EditarAsync(int id, EditarConsultaDto dto)
    {
        var consulta = await _context.ConsultasMedicas.FindAsync(id);
        if (consulta == null) return null;

        consulta.MotivoConsulta = dto.MotivoConsulta;
        consulta.Diagnostico = dto.Diagnostico;
        consulta.Evolucion = dto.Evolucion;
        consulta.Indicaciones = dto.Indicaciones;
        consulta.Observaciones = dto.Observaciones;
        consulta.EstudioAdjuntoUrl = dto.EstudioAdjuntoUrl;

        await _context.SaveChangesAsync();
        return await ObtenerPorIdAsync(id);
    }

    public async Task<List<AntecedenteDto>> ObtenerAntecedentesAsync(int pacienteId)
    {
        return await _context.AntecedentesClinicos
            .Where(a => a.PacienteId == pacienteId)
            .OrderByDescending(a => a.FechaRegistro)
            .Select(a => new AntecedenteDto
            {
                Id = a.Id,
                PacienteId = a.PacienteId,
                Tipo = a.Tipo,
                Descripcion = a.Descripcion,
                FechaRegistro = a.FechaRegistro
            })
            .ToListAsync();
    }

    public async Task<AntecedenteDto> CrearAntecedenteAsync(CrearAntecedenteDto dto)
    {
        var ant = new AntecedenteClinico
        {
            PacienteId = dto.PacienteId,
            Tipo = dto.Tipo,
            Descripcion = dto.Descripcion,
            FechaRegistro = DateTime.UtcNow
        };
        _context.AntecedentesClinicos.Add(ant);
        await _context.SaveChangesAsync();
        return new AntecedenteDto
        {
            Id = ant.Id,
            PacienteId = ant.PacienteId,
            Tipo = ant.Tipo,
            Descripcion = ant.Descripcion,
            FechaRegistro = ant.FechaRegistro
        };
    }

    public async Task<List<AlergiaDto>> ObtenerAlergiasAsync(int pacienteId)
    {
        return await _context.Alergias
            .Where(a => a.PacienteId == pacienteId && a.Activa)
            .Select(a => new AlergiaDto
            {
                Id = a.Id,
                PacienteId = a.PacienteId,
                Sustancia = a.Sustancia,
                Reaccion = a.Reaccion,
                Severidad = a.Severidad,
                Activa = a.Activa
            })
            .ToListAsync();
    }

    public async Task<AlergiaDto> CrearAlergiaAsync(CrearAlergiaDto dto)
    {
        var al = new Alergia
        {
            PacienteId = dto.PacienteId,
            Sustancia = dto.Sustancia,
            Reaccion = dto.Reaccion,
            Severidad = dto.Severidad,
            Activa = true
        };
        _context.Alergias.Add(al);
        await _context.SaveChangesAsync();
        return new AlergiaDto
        {
            Id = al.Id,
            PacienteId = al.PacienteId,
            Sustancia = al.Sustancia,
            Reaccion = al.Reaccion,
            Severidad = al.Severidad,
            Activa = al.Activa
        };
    }
}
