namespace Vitalis.Application.Interfaces;

/// <summary>
/// Traduce entre UTC y la hora local DE LA CLÍNICA.
///
/// Por qué existe: las validaciones de agenda usaban <c>DateTime.ToLocalTime()</c>,
/// que convierte a la hora local de la máquina donde corre el proceso. Con
/// <c>dotnet run</c> en Windows esa máquina estaba en hora argentina y todo
/// funcionaba. Dentro de un contenedor la zona horaria es UTC, así que
/// <c>ToLocalTime()</c> no convertía nada y un turno de las 17:30 se evaluaba
/// como las 20:30: quedaba fuera del horario de atención y se rechazaba.
///
/// El horario de atención pertenece a la clínica, no al servidor donde corre el
/// sistema. Por eso la zona se declara en configuración (<c>Clinica:ZonaHoraria</c>)
/// y no se hereda del entorno.
/// </summary>
public interface IRelojClinica
{
    /// <summary>Convierte un instante UTC a la hora local de la clínica.</summary>
    DateTime AHoraDeLaClinica(DateTime instanteUtc);

    /// <summary>Identificador de la zona horaria en uso, para diagnóstico.</summary>
    string ZonaHoraria { get; }
}
