using System;

namespace Vitalis.Domain.Entities;

public class Auditoria
{
    public int Id { get; set; }
    public string? UsuarioEmail { get; set; }
    public string Accion { get; set; } = string.Empty; // "CREAR", "MODIFICAR", "ELIMINAR"
    public string Tabla { get; set; } = string.Empty;
    public string ClavePrimaria { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? ValoresAnteriores { get; set; } // Representación JSON de datos anteriores
    public string? ValoresNuevos { get; set; } // Representación JSON de datos nuevos
}
