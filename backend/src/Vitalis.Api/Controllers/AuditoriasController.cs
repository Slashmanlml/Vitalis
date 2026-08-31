using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Vitalis.Application.DTOs.Auditorias;
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

    /// <summary>
    /// Tablas cuyo contenido es materia de secreto profesional. De sus registros
    /// de auditoría se informa QUIÉN hizo QUÉ y CUÁNDO, pero no el detalle.
    ///
    /// El administrador no tiene acceso a la historia clínica. Sin este filtro
    /// la tendría igual: el registro de auditoría guarda el JSON completo de la
    /// fila modificada, diagnóstico incluido. Restringir la puerta principal y
    /// dejar abierta la ventana no restringe nada.
    /// </summary>
    private static readonly HashSet<string> TablasClinicas = new(StringComparer.OrdinalIgnoreCase)
    {
        "consultas_medicas",
        "prescripciones",
        "prescripcion_detalles",
        "antecedentes_clinicos",
        "alergias"
    };

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas()
    {
        var auditorias = await _context.Auditorias
            .AsNoTracking()
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();

        var resultado = auditorias.Select(a =>
        {
            var esClinica = TablasClinicas.Contains(a.Tabla);

            return new AuditoriaDto
            {
                Id = a.Id,
                UsuarioEmail = a.UsuarioEmail,
                Accion = a.Accion,
                Tabla = a.Tabla,
                ClavePrimaria = a.ClavePrimaria,
                Fecha = a.Fecha,

                // La trazabilidad se conserva entera: se sigue viendo que tal
                // usuario modificó tal registro de tal tabla en tal momento. Lo
                // único que no se devuelve es el contenido.
                ValoresAnteriores = esClinica ? null : a.ValoresAnteriores,
                ValoresNuevos = esClinica ? null : a.ValoresNuevos,
                ContenidoClinicoOculto = esClinica
            };
        });

        return Ok(resultado);
    }
}
