namespace Vitalis.Domain.Entities;

public class Alergia
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public string Sustancia { get; set; } = string.Empty;
    public string? Reaccion { get; set; }
    public string? Severidad { get; set; }
    public bool Activa { get; set; } = true;
}
