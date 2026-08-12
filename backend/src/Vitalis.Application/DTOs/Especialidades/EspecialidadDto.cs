using System.ComponentModel.DataAnnotations;
namespace Vitalis.Application.DTOs.Especialidades;

public class EspecialidadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class CrearEspecialidadDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class EditarEspecialidadDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
