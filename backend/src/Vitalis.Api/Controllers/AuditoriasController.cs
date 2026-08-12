using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Vitalis.Domain.Constants;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador)]
public class AuditoriasController : ControllerBase
{
    private readonly VitalisDbContext _context;

    public AuditoriasController(VitalisDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var auditorias = await _context.Auditorias
            .AsNoTracking()
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();

        return Ok(auditorias);
    }
}
