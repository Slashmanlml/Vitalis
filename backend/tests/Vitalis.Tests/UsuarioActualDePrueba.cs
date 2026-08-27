using System.Threading;
using System.Threading.Tasks;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Tests;

/// <summary>
/// Doble de prueba de <see cref="IUsuarioActual"/>.
///
/// Por defecto simula un administrador: sin restricciones, que es como se
/// comportaba el sistema antes de incorporar el control por profesional. Asi las
/// pruebas ya existentes siguen describiendo el mismo escenario.
///
/// Para probar el control de acceso, poner Rol = Roles.Medico y ProfesionalId
/// con el id del profesional que se quiere simular.
/// </summary>
public class UsuarioActualDePrueba : IUsuarioActual
{
    public int? UsuarioId { get; set; } = 1;

    public string? Rol { get; set; } = Roles.Administrador;

    public int? ProfesionalId { get; set; }

    public bool EsMedico => Rol == Roles.Medico;

    public Task<int?> ObtenerProfesionalIdAsync(CancellationToken ct = default)
        => Task.FromResult(ProfesionalId);
}
