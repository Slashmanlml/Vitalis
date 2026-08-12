namespace Vitalis.Application.DTOs.Consultas;

public class AntecedenteDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}

public class CrearAntecedenteDto
{
    public int PacienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class AlergiaDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string Sustancia { get; set; } = string.Empty;
    public string? Reaccion { get; set; }
    public string? Severidad { get; set; }
    public bool Activa { get; set; }
}

public class CrearAlergiaDto
{
    public int PacienteId { get; set; }
    public string Sustancia { get; set; } = string.Empty;
    public string? Reaccion { get; set; }
    public string? Severidad { get; set; }
}
