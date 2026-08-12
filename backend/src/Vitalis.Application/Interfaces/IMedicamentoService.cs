using Vitalis.Application.DTOs.Medicamentos;

namespace Vitalis.Application.Interfaces;

public interface IMedicamentoService
{
    Task<List<MedicamentoDto>> ObtenerTodosAsync(string? buscar = null);
    Task<MedicamentoDto?> ObtenerPorIdAsync(int id);
    Task<MedicamentoDto> CrearAsync(CrearMedicamentoDto dto);
    Task<MedicamentoDto?> EditarAsync(int id, EditarMedicamentoDto dto);
    Task<bool> EliminarAsync(int id);
}
