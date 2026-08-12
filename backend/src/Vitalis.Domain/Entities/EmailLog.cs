namespace Vitalis.Domain.Entities;

public class EmailLog
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
}
