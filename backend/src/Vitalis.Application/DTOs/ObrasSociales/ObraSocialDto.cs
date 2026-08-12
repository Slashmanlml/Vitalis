using System.ComponentModel.DataAnnotations;

namespace Vitalis.Application.DTOs.ObrasSociales;

public class ObraSocialDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool Activa { get; set; }
}
