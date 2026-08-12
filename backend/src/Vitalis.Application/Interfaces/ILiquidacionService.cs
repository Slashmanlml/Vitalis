using Vitalis.Application.DTOs.Liquidaciones;

namespace Vitalis.Application.Interfaces;

public interface ILiquidacionService
{
    Task<List<LiquidacionDto>> ObtenerTodasAsync();
    Task<LiquidacionDto?> ObtenerPorIdAsync(int id);
    Task<LiquidacionDto> CrearAsync(CrearLiquidacionDto dto);
    Task<LiquidacionDto?> LiquidarAsync(int id);
}
