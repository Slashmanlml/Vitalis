using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador + "," + Roles.Medico + "," + Roles.Recepcionista + "," + Roles.Facturacion)]
public class SearchController : ControllerBase
{
    private readonly ISearchService _service;
    public SearchController(ISearchService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { pacientes = new List<object>(), profesionales = new List<object>(), turnos = new List<object>() });
        return Ok(await _service.BuscarAsync(q));
    }
}
