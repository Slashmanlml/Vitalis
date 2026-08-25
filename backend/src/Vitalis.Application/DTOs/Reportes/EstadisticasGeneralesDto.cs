namespace Vitalis.Application.DTOs.Reportes;

/// <summary>
/// Estadísticas agregadas de la agenda. Antes este endpoint devolvía un
/// <c>object</c> anónimo: el frontend no tenía contrato y los tests debían
/// leerlo por reflexión. Tiparlo permite consumirlo y probarlo directamente.
/// </summary>
public class EstadisticasGeneralesDto
{
    public int TotalTurnos { get; set; }
    public int Confirmados { get; set; }
    public int Pendientes { get; set; }
    public int Atendidos { get; set; }
    public int Cancelados { get; set; }

    public List<ConteoPorCategoriaDto> PorEspecialidad { get; set; } = new();
    public List<ConteoPorCategoriaDto> PorObraSocial { get; set; } = new();
    public List<ConteoPorCategoriaDto> PorProfesional { get; set; } = new();
    public List<ConteoPorCategoriaDto> PorMes { get; set; } = new();
}

/// <summary>Par etiqueta/cantidad, pensado para alimentar un gráfico.</summary>
public class ConteoPorCategoriaDto
{
    public string Etiqueta { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}
