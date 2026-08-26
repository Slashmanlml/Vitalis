using System.ComponentModel.DataAnnotations;

namespace Vitalis.Application.DTOs.Emails;

public class SimularEmailDto
{
    [Required(ErrorMessage = "El destinatario es obligatorio")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    public string Destinatario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de notificación es obligatorio")]
    public string TipoNotificacion { get; set; } = "Personalizado";

    public string? Asunto { get; set; }
    public string? Cuerpo { get; set; }
}
