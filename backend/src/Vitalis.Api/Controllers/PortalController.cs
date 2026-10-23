using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.DTOs.Portal;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

/// <summary>
/// Portal del paciente: lo que ve una persona que se registró desde la web.
/// Solo lectura y solo de lo propio. Ninguna ruta recibe un id de paciente.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Paciente)]
public class PortalController(IPortalPacienteService portal) : ControllerBase
{
    [HttpGet("mi-perfil")]
    public async Task<ActionResult<MiPerfilPacienteDto>> MiPerfil(CancellationToken ct) =>
        Ok(await portal.ObtenerMiPerfilAsync(ct));

    [HttpGet("mis-turnos")]
    public async Task<ActionResult<IEnumerable<MiTurnoDto>>> MisTurnos(CancellationToken ct) =>
        Ok(await portal.ObtenerMisTurnosAsync(ct));
}
