namespace Vitalis.Domain.Constants;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Medico = "Medico";
    public const string Recepcionista = "Recepcionista";
    public const string Facturacion = "Facturacion";
    public const string Paciente = "Paciente";

    /// <summary>
    /// Todos los roles del personal del consultorio, separados por coma para
    /// usar en [Authorize(Roles = ...)]. Existe porque, al habilitar el
    /// registro público de pacientes, un [Authorize] "a secas" deja de
    /// significar "alguien del consultorio" y pasa a incluir a cualquier
    /// persona que se haya creado una cuenta desde la web.
    /// </summary>
    public const string PersonalClinica = Administrador + "," + Medico + "," + Recepcionista + "," + Facturacion;

    public static readonly string[] Todos =
    [
        Administrador,
        Medico,
        Recepcionista,
        Facturacion,
        Paciente
    ];
}
