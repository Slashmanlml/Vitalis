using Vitalis.Application.DTOs.Facturas;

namespace Vitalis.Application.Interfaces;

public interface IFacturaService
{
    Task<List<FacturaDto>> ObtenerTodasAsync();
    Task<List<FacturaDto>> ObtenerPorPacienteAsync(int pacienteId);
    Task<FacturaDto?> ObtenerPorIdAsync(int id);
    Task<FacturaDto> CrearAsync(CrearFacturaDto dto);
    Task<FacturaDto> RegistrarPagoAsync(RegistrarPagoDto dto);
}
