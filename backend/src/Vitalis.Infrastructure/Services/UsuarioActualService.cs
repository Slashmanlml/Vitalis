using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class UsuarioActualService : IUsuarioActual
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly VitalisDbContext _context;

    public UsuarioActualService(IHttpContextAccessor httpContextAccessor, VitalisDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    private ClaimsPrincipal? Usuario => _httpContextAccessor.HttpContext?.User;

    public int? UsuarioId
    {
        get
        {
            // El token guarda el id del usuario en "sub"; ASP.NET Core lo expone
            // como NameIdentifier. Se aceptan ambos por si cambia el mapeo.
            var valor = Usuario?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Usuario?.FindFirstValue("sub");

            return int.TryParse(valor, out var id) && id > 0 ? id : null;
        }
    }

    public string? Rol => Usuario?.FindFirstValue(ClaimTypes.Role);

    public bool EsMedico => string.Equals(Rol, Roles.Medico, StringComparison.OrdinalIgnoreCase);

    public async Task<int?> ObtenerProfesionalIdAsync(CancellationToken ct = default)
    {
        var usuarioId = UsuarioId;
        if (usuarioId is null)
        {
            return null;
        }

        return await _context.Profesionales
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId && p.Activo)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);
    }
}
