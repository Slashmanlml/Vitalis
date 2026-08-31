using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitalis.Application.Interfaces;

namespace Vitalis.Infrastructure.Configuracion;

public class RelojClinica : IRelojClinica
{
    private readonly TimeZoneInfo _zona;

    public RelojClinica(IOptions<ClinicaOptions> opciones, ILogger<RelojClinica> logger)
    {
        var configurada = opciones.Value.ZonaHoraria;

        try
        {
            _zona = TimeZoneInfo.FindSystemTimeZoneById(configurada);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Falla ruidosamente y sigue con UTC. Quedarse callado seria peor:
            // el sistema andaria con el horario de atencion corrido y nadie
            // sabria por que, que es exactamente el problema que este servicio
            // vino a resolver.
            logger.LogError(ex,
                "La zona horaria '{Zona}' no existe en este sistema. Se usa UTC. " +
                "Revise Clinica:ZonaHoraria en la configuracion.", configurada);
            _zona = TimeZoneInfo.Utc;
        }
    }

    public string ZonaHoraria => _zona.Id;

    public DateTime AHoraDeLaClinica(DateTime instanteUtc)
    {
        // Si viene sin Kind se asume UTC: es como se guarda todo en la base.
        var utc = instanteUtc.Kind == DateTimeKind.Utc
            ? instanteUtc
            : DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, _zona);
    }
}
