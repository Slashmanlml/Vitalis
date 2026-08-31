namespace Vitalis.Infrastructure.Configuracion;

public class ClinicaOptions
{
    /// <summary>
    /// Zona horaria de la clínica, en formato IANA. .NET 8 acepta identificadores
    /// IANA tanto en Linux como en Windows, así que el mismo valor sirve para
    /// desarrollo y para el contenedor.
    /// </summary>
    public string ZonaHoraria { get; set; } = "America/Argentina/Buenos_Aires";

    /// <summary>Hora de apertura, en hora de la clínica.</summary>
    public int HoraApertura { get; set; } = 8;

    /// <summary>Hora de cierre, en hora de la clínica.</summary>
    public int HoraCierre { get; set; } = 20;
}
