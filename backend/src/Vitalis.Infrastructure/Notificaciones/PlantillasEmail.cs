using System.Net;
using Vitalis.Domain.Constants;

namespace Vitalis.Infrastructure.Notificaciones;

public static class PlantillasEmail
{
    public static (string Asunto, string CuerpoHtml) Generar(string evento, Dictionary<string, string> datos)
    {
        string pacNombre = ObtenerDato(datos, "PacienteNombre", "Estimado/a paciente");
        string profNombre = ObtenerDato(datos, "ProfesionalNombre", "Profesional Médico");
        string fechaHora = ObtenerDato(datos, "FechaHora", "");
        string especialidad = ObtenerDato(datos, "Especialidad", "Consulta General");

        switch (evento)
        {
            case EventoNotificacion.TurnoCreado:
            {
                string asunto = "Reserva de Turno Médico Registrada - Vitalis";
                string cuerpo = FormatearContenedor(
                    "¡Turno Reservado con Éxito!",
                    "#0f766e",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>Le confirmamos que se ha registrado su solicitud de turno en nuestro consultorio.</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p><strong>Médico:</strong> Dr/Dra. {WebUtility.HtmlEncode(profNombre)}</p>
                    <p><strong>Especialidad:</strong> {WebUtility.HtmlEncode(especialidad)}</p>
                    <p><strong>Fecha y Hora:</strong> {WebUtility.HtmlEncode(fechaHora)}</p>
                    <p><strong>Estado:</strong> Solicitado / Pendiente de confirmación</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 13px; color: #475569;'>Si necesita reprogramar o cancelar su turno, puede hacerlo desde el portal de autogestión.</p>"
                );
                return (asunto, cuerpo);
            }

            case EventoNotificacion.TurnoReprogramado:
            {
                string fechaAnterior = ObtenerDato(datos, "FechaAnterior", "");
                string asunto = "Su Turno Médico fue Reprogramado - Vitalis";
                string bloqueAnterior = string.IsNullOrWhiteSpace(fechaAnterior)
                    ? ""
                    : $@"<p style='color:#94a3b8;'><strong>Fecha anterior:</strong> <s>{WebUtility.HtmlEncode(fechaAnterior)}</s></p>";
                string cuerpo = FormatearContenedor(
                    "Su Turno fue Reprogramado",
                    "#b45309",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>Le informamos que la fecha de su turno ha sido modificada. Por favor, tome nota del nuevo horario.</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p><strong>Médico:</strong> Dr/Dra. {WebUtility.HtmlEncode(profNombre)}</p>
                    <p><strong>Especialidad:</strong> {WebUtility.HtmlEncode(especialidad)}</p>
                    {bloqueAnterior}
                    <p><strong>Nueva fecha y hora:</strong> {WebUtility.HtmlEncode(fechaHora)}</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 13px; color: #475569;'>Si el nuevo horario no le resulta conveniente, comuníquese con el consultorio para reprogramarlo nuevamente.</p>"
                );
                return (asunto, cuerpo);
            }

            case EventoNotificacion.TurnoConfirmado:
            {
                string asunto = "Turno Médico Confirmado Oficialmente - Vitalis";
                string cuerpo = FormatearContenedor(
                    "¡Su Turno ha sido Confirmado!",
                    "#0f766e",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>Nos complace informarle que su turno médico ha sido <strong>confirmado en la agenda oficial</strong>.</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p><strong>Profesional:</strong> Dr/Dra. {WebUtility.HtmlEncode(profNombre)} ({WebUtility.HtmlEncode(especialidad)})</p>
                    <p><strong>Fecha y Hora:</strong> {WebUtility.HtmlEncode(fechaHora)}</p>
                    <p><strong>Ubicación:</strong> Consultorio Central / Atención Virtual Vitalis</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 13px; color: #475569;'>Recomendamos presentarse con 10 minutos de antelación con su DNI y credencial médica.</p>"
                );
                return (asunto, cuerpo);
            }

            case EventoNotificacion.RecordatorioTurno:
            {
                string horas = ObtenerDato(datos, "HorasRestantes", "24");
                string asunto = $"Recordatorio: Su cita médica es en {horas} horas - Vitalis";
                string cuerpo = FormatearContenedor(
                    "Recordatorio de Consulta Médica",
                    "#d97706",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>Le recordamos que tiene una consulta médica programada para las próximas <strong>{horas} horas</strong>.</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p><strong>Profesional:</strong> Dr/Dra. {WebUtility.HtmlEncode(profNombre)} ({WebUtility.HtmlEncode(especialidad)})</p>
                    <p><strong>Fecha y Hora:</strong> {WebUtility.HtmlEncode(fechaHora)}</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 13px; color: #475569;'>Si no puede asistir, por favor infórmenos con anticipación para ceder el turno a otro paciente en espera.</p>"
                );
                return (asunto, cuerpo);
            }

            case EventoNotificacion.TurnoCancelado:
            {
                string motivo = ObtenerDato(datos, "Motivo", "");
                string motivoHtml = string.IsNullOrWhiteSpace(motivo) ? "" : $"<p><strong>Motivo:</strong> {WebUtility.HtmlEncode(motivo)}</p>";
                string asunto = "Aviso de Cancelación de Turno - Vitalis";
                string cuerpo = FormatearContenedor(
                    "Aviso de Cancelación de Turno",
                    "#e11d48",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>Le informamos que el turno programado para el día <strong>{WebUtility.HtmlEncode(fechaHora)}</strong> con el profesional <strong>Dr/Dra. {WebUtility.HtmlEncode(profNombre)}</strong> ha sido cancelado.</p>
                    {motivoHtml}
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 13px; color: #475569;'>Puede solicitar un nuevo turno cuando lo desee ingresando a nuestra plataforma o contactando a recepción.</p>"
                );
                return (asunto, cuerpo);
            }

            case EventoNotificacion.ResumenConsulta:
            {
                string indicaciones = ObtenerDato(datos, "Indicaciones", "Seguir las pautas acordadas en consulta.");
                string asunto = "Resumen de Atención Médica - Vitalis";
                string cuerpo = FormatearContenedor(
                    "Resumen de Consulta Médica",
                    "#0f766e",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>Compartimos las indicaciones registradas por el profesional <strong>Dr/Dra. {WebUtility.HtmlEncode(profNombre)}</strong> en su atención médica del <strong>{WebUtility.HtmlEncode(fechaHora)}</strong>.</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p><strong>Indicaciones y Recomendaciones:</strong></p>
                    <div style='background: #f1f5f9; padding: 12px 16px; border-radius: 6px; font-style: italic; color: #334155;'>
                        {WebUtility.HtmlEncode(indicaciones)}
                    </div>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 12px; color: #64748b;'>Por razones de privacidad y confidencialidad médica, diagnósticos y estudios detallados se encuentran resguardados en su Historia Clínica Electrónica.</p>"
                );
                return (asunto, cuerpo);
            }

            case EventoNotificacion.NuevaPrescripcion:
            {
                string detalleMeds = ObtenerDato(datos, "DetalleMedicamentos", "");
                string observaciones = ObtenerDato(datos, "Observaciones", "");
                string obsHtml = string.IsNullOrWhiteSpace(observaciones) ? "" : $"<p><strong>Observaciones:</strong> {WebUtility.HtmlEncode(observaciones)}</p>";
                string asunto = "Nueva Receta Médica Electrónica - Vitalis";
                string cuerpo = FormatearContenedor(
                    "Receta Médica Emitida",
                    "#0f766e",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>El profesional <strong>Dr/Dra. {WebUtility.HtmlEncode(profNombre)}</strong> ha emitido una orden de medicamentos para su tratamiento.</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p><strong>Medicamentos prescriptos:</strong></p>
                    {detalleMeds}
                    {obsHtml}
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 13px; color: #475569;'>Puede presentar esta constancia en farmacia o consultar el folio digital en su portal de paciente.</p>"
                );
                return (asunto, cuerpo);
            }

            case EventoNotificacion.BienvenidaPaciente:
            {
                string asunto = "¡Bienvenido/a al Portal Médico Vitalis!";
                string cuerpo = FormatearContenedor(
                    "Bienvenido/a a Vitalis",
                    "#0284c7",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <p>Se ha creado con éxito su ficha clínica en nuestro sistema de consultorios médicos virtuales.</p>
                    <p>A partir de ahora podrá gestionar sus turnos, acceder a sus recetas médicas y consultar su historial de forma digital y segura.</p>
                    <hr style='border:0; border-top:1px solid #e2e8f0; margin: 15px 0;'/>
                    <p style='font-size: 13px; color: #475569;'>Si tiene dudas o consultas, nuestro equipo de recepción está a su entera disposición.</p>"
                );
                return (asunto, cuerpo);
            }

            default:
            {
                string asunto = ObtenerDato(datos, "Asunto", "Notificación Informativa - Vitalis");
                string contenido = ObtenerDato(datos, "Cuerpo", "Estimado/a paciente, le enviamos un mensaje informativo sobre sus consultas médicas.");
                string cuerpo = FormatearContenedor(
                    "Notificación del Consultorio",
                    "#0f766e",
                    $@"<p>Estimado/a <strong>{WebUtility.HtmlEncode(pacNombre)}</strong>,</p>
                    <div style='padding: 10px 0;'>{contenido}</div>"
                );
                return (asunto, cuerpo);
            }
        }
    }

    private static string ObtenerDato(Dictionary<string, string> datos, string clave, string valorPorDefecto)
    {
        return datos != null && datos.TryGetValue(clave, out var val) && !string.IsNullOrWhiteSpace(val) 
            ? val 
            : valorPorDefecto;
    }

    private static string FormatearContenedor(string titulo, string colorCabecera, string contenidoHtml)
    {
        return $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #1e293b; background: #f8fafc; border-radius: 8px; overflow: hidden; border: 1px solid #e2e8f0;'>
            <div style='background: {colorCabecera}; color: #ffffff; padding: 20px; text-align: center;'>
                <h2 style='margin: 0; font-size: 20px; font-weight: 700;'>{WebUtility.HtmlEncode(titulo)}</h2>
            </div>
            <div style='padding: 24px; background: #ffffff;'>
                {contenidoHtml}
            </div>
            <div style='background: #f1f5f9; padding: 14px 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;'>
                <strong>Vitalis Consultorios Médicos</strong> · Plataforma Integral de Salud
            </div>
        </div>";
    }
}
