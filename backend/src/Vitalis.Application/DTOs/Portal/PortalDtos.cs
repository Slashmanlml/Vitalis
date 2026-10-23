namespace Vitalis.Application.DTOs.Portal;

/// <summary>Lo que ve el paciente de su propia ficha en el portal.</summary>
public class MiPerfilPacienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? ObraSocialNombre { get; set; }
    public string? NumeroAfiliado { get; set; }
}

/// <summary>
/// Un turno visto desde el portal. A propósito no lleva ids internos de
/// profesional ni de obra social: el paciente no los necesita y no hay por
/// qué exponerlos.
/// </summary>
public class MiTurnoDto
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool Confirmado { get; set; }
}
