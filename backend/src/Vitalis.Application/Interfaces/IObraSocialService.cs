using Vitalis.Application.DTOs.ObrasSociales;

namespace Vitalis.Application.Interfaces;

public interface IObraSocialService
{
    Task<IEnumerable<ObraSocialDto>> ObtenerTodasAsync();
    Task<ObraSocialDto?> ObtenerPorIdAsync(int id);
    Task<ObraSocialDto> CrearAsync(CrearObraSocialDto dto);
    Task<ObraSocialDto?> EditarAsync(int id, EditarObraSocialDto dto);
    Task<bool> EliminarAsync(int id);
}
