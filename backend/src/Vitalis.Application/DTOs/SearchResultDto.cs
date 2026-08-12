namespace Vitalis.Application.DTOs;

public class SearchResultDto
{
    public List<SearchItemDto> Pacientes { get; set; } = new();
    public List<SearchItemDto> Profesionales { get; set; } = new();
    public List<SearchItemDto> Turnos { get; set; } = new();
}

public class SearchItemDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
}
