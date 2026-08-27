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
    private readonly IEmailService _emailService;
    private readonly IUsuarioActual _usuarioActual;

    public PrescripcionService(
        VitalisDbContext context,
        IEmailService emailService,
        IUsuarioActual usuarioActual)
    {
        _context = context;
        _emailService = emailService;
        _usuarioActual = usuarioActual;
    }

    /// <summary>
    /// Una receta la firma el medico que atendio. Un medico no puede emitir
    /// recetas sobre consultas de otro profesional.
    /// </summary>
    private async Task VerificarQuePuedeOperarSobreAsync(int profesionalDeLaConsultaId)
    {
        if (!_usuarioActual.EsMedico)
        {
            return;
        }

        var miProfesionalId = await _usuarioActual.ObtenerProfesionalIdAsync();

        if (miProfesionalId is null)
        {
            throw new ForbiddenException(
                "Su usuario no esta vinculado a una ficha profesional, por lo que no puede emitir recetas.");
        }

        if (miProfesionalId != profesionalDeLaConsultaId)
        {
            throw new ForbiddenException(
                "La consulta pertenece a otro profesional. Solo el medico tratante puede emitir la receta.");
        }
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
        var consulta = await _context.ConsultasMedicas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.ConsultaMedicaId)
            ?? throw new NotFoundException("Consulta médica no encontrada.");

        // Un medico solo receta sobre sus propias consultas.
        await VerificarQuePuedeOperarSobreAsync(consulta.ProfesionalId);

        foreach (var detalle in dto.Detalles)
        {
            var medicamentoExiste = await _context.Medicamentos.AnyAsync(m => m.Id == detalle.MedicamentoId);
            if (!medicamentoExiste)
            {
                throw new NotFoundException($"Medicamento con id {detalle.MedicamentoId} no encontrado.");
            }
        }

        // Paciente y profesional salen DE LA CONSULTA, no del cuerpo del pedido:
        // una receta no puede quedar atribuida a un medico que no atendio, ni
        // emitida a nombre de un paciente que no fue el de la consulta.
        var presc = new Prescripcion
        {
            ConsultaMedicaId = consulta.Id,
            PacienteId = consulta.PacienteId,
            ProfesionalId = consulta.ProfesionalId,
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

        var pac = await _context.Pacientes.FindAsync(presc.PacienteId);
        var prof = await _context.Profesionales.FindAsync(presc.ProfesionalId);
        if (pac != null && !string.IsNullOrWhiteSpace(pac.Email) && prof != null)
        {
            var meds = await _context.Medicamentos
                .Where(m => dto.Detalles.Select(d => d.MedicamentoId).Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Nombre);

            string medsHtml = string.Join("", dto.Detalles.Select(d => 
                $"<li><strong>{(meds.ContainsKey(d.MedicamentoId) ? meds[d.MedicamentoId] : "Medicamento")}</strong>: {d.Dosis} - {d.Frecuencia} por {d.Duracion}</li>"));

            await _emailService.NotificarAsync(new Application.DTOs.Emails.NotificacionRequest
            {
                Destinatario = pac.Email,
                Evento = Domain.Constants.EventoNotificacion.NuevaPrescripcion,
                Datos = new Dictionary<string, string>
                {
                    ["PacienteNombre"] = $"{pac.Nombre} {pac.Apellido}",
                    ["ProfesionalNombre"] = $"{prof.Nombre} {prof.Apellido}",
                    ["DetalleMedicamentos"] = $"<ul>{medsHtml}</ul>",
                    ["Observaciones"] = dto.Observaciones ?? ""
                }
            });
        }

        return await ObtenerPorIdAsync(presc.Id) ?? throw new Exception("Error al crear prescripcion");
    }
}
