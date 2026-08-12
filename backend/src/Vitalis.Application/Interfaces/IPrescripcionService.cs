using Vitalis.Application.DTOs.Prescripciones;

namespace Vitalis.Application.Interfaces;

public interface IPrescripcionService
{
    Task<List<PrescripcionDto>> ObtenerPorPacienteAsync(int pacienteId);
    Task<PrescripcionDto?> ObtenerPorIdAsync(int id);
    Task<PrescripcionDto> CrearAsync(CrearPrescripcionDto dto);
}
