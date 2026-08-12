using Vitalis.Application.DTOs.Consultas;

namespace Vitalis.Application.Interfaces;

public interface IConsultaMedicaService
{
    Task<List<ConsultaMedicaDto>> ObtenerPorPacienteAsync(int pacienteId);
    Task<ConsultaMedicaDto?> ObtenerPorIdAsync(int id);
    Task<ConsultaMedicaDto> CrearAsync(CrearConsultaDto dto);
    Task<ConsultaMedicaDto?> EditarAsync(int id, EditarConsultaDto dto);
    Task<List<AntecedenteDto>> ObtenerAntecedentesAsync(int pacienteId);
    Task<AntecedenteDto> CrearAntecedenteAsync(CrearAntecedenteDto dto);
    Task<List<AlergiaDto>> ObtenerAlergiasAsync(int pacienteId);
    Task<AlergiaDto> CrearAlergiaAsync(CrearAlergiaDto dto);
}
