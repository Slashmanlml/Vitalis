namespace Vitalis.Application.DTOs.Auditorias;

/// <summary>
/// Un registro de auditoría tal como se muestra en pantalla.
///
/// No se devuelve la entidad directamente porque el registro guarda, en
/// <c>ValoresAnteriores</c> y <c>ValoresNuevos</c>, el JSON completo de la fila
/// afectada. Para las tablas clínicas eso incluye diagnósticos, evolución e
/// indicaciones. La pantalla de auditorías es exclusiva del administrador, que
/// no tiene acceso a la historia clínica: devolver ese JSON le daría por esta
/// vía lo que se le niega por la puerta principal.
/// </summary>
public class AuditoriaDto
{
    public int Id { get; set; }
    public string? UsuarioEmail { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Tabla { get; set; } = string.Empty;
    public string ClavePrimaria { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }

    public string? ValoresAnteriores { get; set; }
    public string? ValoresNuevos { get; set; }

    /// <summary>
    /// True cuando el detalle se omitió por tratarse de una tabla clínica. La
    /// pantalla lo usa para explicar por qué no hay detalle, en lugar de mostrar
    /// un espacio vacío que parece un error.
    /// </summary>
    public bool ContenidoClinicoOculto { get; set; }
}
