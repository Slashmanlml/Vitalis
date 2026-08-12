using Vitalis.Application.DTOs.Prestaciones;

namespace Vitalis.Application.Interfaces;

public interface IPrestacionService
{
    Task<List<PrestacionDto>> ObtenerTodasAsync();
    Task<PrestacionDto?> ObtenerPorIdAsync(int id);
    Task<PrestacionDto> CrearAsync(CrearPrestacionDto dto);
    Task<PrestacionDto?> EditarAsync(int id, EditarPrestacionDto dto);
    Task<bool> EliminarAsync(int id);
}
