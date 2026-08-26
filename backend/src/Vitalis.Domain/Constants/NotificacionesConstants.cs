namespace Vitalis.Domain.Constants;

public static class OrigenNotificacion
{
    public const string Sistema = "Sistema";
    public const string Simulado = "Simulado";
}

public static class EventoNotificacion
{
    public const string TurnoCreado = "TurnoCreado";
    public const string TurnoConfirmado = "TurnoConfirmado";
    public const string TurnoCancelado = "TurnoCancelado";
    public const string TurnoReprogramado = "TurnoReprogramado";
    public const string RecordatorioTurno = "RecordatorioTurno";
    public const string ResumenConsulta = "ResumenConsulta";
    public const string NuevaPrescripcion = "NuevaPrescripcion";
    public const string BienvenidaPaciente = "BienvenidaPaciente";
    public const string Personalizado = "Personalizado";
}

public static class EstadoNotificacion
{
    public const string Enviado = "Enviado";
    public const string Fallido = "Fallido";
    public const string Simulado = "Simulado";
}
