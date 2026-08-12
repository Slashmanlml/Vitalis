namespace Vitalis.Domain.Constants;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Medico = "Medico";
    public const string Recepcionista = "Recepcionista";
    public const string Facturacion = "Facturacion";
    public const string Paciente = "Paciente";

    public static readonly string[] Todos =
    [
        Administrador,
        Medico,
        Recepcionista,
        Facturacion,
        Paciente
    ];
}
