namespace Vitalis.Domain.Constants;

/// <summary>
/// Estados de un turno y los pasos permitidos entre ellos. Antes el estado
/// era texto libre: se podía mandar "Atendido" → "Solicitado", o cualquier
/// palabra, y el turno quedaba en un estado que ninguna pantalla entiende
/// (y que, por ejemplo, desaparecía de la liquidación del médico).
/// </summary>
public static class EstadosTurno
{
    public const string Solicitado = "Solicitado";
    public const string Confirmado = "Confirmado";
    public const string EnEspera = "En Espera";
    public const string EnAtencion = "En Atencion";
    public const string Atendido = "Atendido";
    public const string Cancelado = "Cancelado";

    /// <summary>
    /// Desde cada estado, a cuáles se puede pasar. Se permite volver un paso
    /// atrás en la sala de espera (un error de clic), pero un turno atendido o
    /// cancelado ya no cambia: es parte del historial clínico y de la facturación.
    /// </summary>
    private static readonly Dictionary<string, string[]> Permitidos = new()
    {
        [Solicitado] = new[] { Confirmado, EnEspera, Cancelado },
        [Confirmado] = new[] { Solicitado, EnEspera, Cancelado },
        [EnEspera] = new[] { Confirmado, EnAtencion, Cancelado },
        [EnAtencion] = new[] { EnEspera, Atendido },
        [Atendido] = Array.Empty<string>(),
        [Cancelado] = Array.Empty<string>()
    };

    public static bool EsValido(string estado) => Permitidos.ContainsKey(estado);

    public static bool EsFinal(string estado) => estado is Atendido or Cancelado;

    public static bool PuedePasar(string desde, string hacia) =>
        desde == hacia || (Permitidos.TryGetValue(desde, out var destinos) && destinos.Contains(hacia));
}
