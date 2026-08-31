using System;
using Vitalis.Application.Interfaces;

namespace Vitalis.Tests;

/// <summary>
/// Doble de prueba de <see cref="IRelojClinica"/> con un desplazamiento fijo.
///
/// Por defecto usa -3 horas, la hora argentina, de modo que las pruebas describen
/// el escenario real: la clínica está en Argentina y el servidor puede estar en
/// cualquier parte. No se usa la zona horaria real del sistema a propósito — eso
/// haría que las pruebas pasaran o fallaran según la máquina, que es exactamente
/// el defecto que este servicio vino a corregir.
/// </summary>
public class RelojDePrueba : IRelojClinica
{
    public TimeSpan Desfase { get; set; } = TimeSpan.FromHours(-3);

    public string ZonaHoraria => $"UTC{Desfase.Hours:+00;-00}:00 (prueba)";

    public DateTime AHoraDeLaClinica(DateTime instanteUtc)
    {
        var utc = instanteUtc.Kind == DateTimeKind.Utc
            ? instanteUtc
            : DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc);

        return DateTime.SpecifyKind(utc + Desfase, DateTimeKind.Unspecified);
    }
}
