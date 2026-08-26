using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitalis.Application.DTOs.Emails;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Notificaciones;

public class RecordatorioTurnosService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificacionesOptions _options;
    private readonly ILogger<RecordatorioTurnosService> _logger;

    public RecordatorioTurnosService(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificacionesOptions> options,
        ILogger<RecordatorioTurnosService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Recordatorio de Turnos iniciado. Frecuencia de barrido: {Minutos} minutos.", _options.MinutosEntreBarridos);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarRecordatoriosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el barrido de recordatorios automáticos.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_options.MinutosEntreBarridos), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Servicio de Recordatorio de Turnos detenido.");
    }

    public async Task<int> ProcesarRecordatoriosAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VitalisDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var ahora = DateTime.UtcNow;
        var desde = ahora.AddHours(_options.HorasAntesDelRecordatorio);
        var hasta = desde.AddMinutes(_options.MinutosEntreBarridos);

        var candidatos = await context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Profesional).ThenInclude(p => p!.Especialidad)
            .Where(t => t.FechaHora >= desde
                     && t.FechaHora < hasta
                     && t.Estado != "Cancelado"
                     && t.Paciente != null
                     && t.Paciente.Email != null
                     && !context.EmailLogs.Any(l => l.TurnoId == t.Id
                                                  && l.Evento == EventoNotificacion.RecordatorioTurno
                                                  && l.Estado == EstadoNotificacion.Enviado))
            .ToListAsync(ct);

        if (!candidatos.Any())
        {
            return 0;
        }

        _logger.LogInformation("Se encontraron {Count} turnos para procesar recordatorios.", candidatos.Count);
        int enviados = 0;

        foreach (var turno in candidatos)
        {
            if (turno.Paciente == null || string.IsNullOrWhiteSpace(turno.Paciente.Email))
            {
                continue;
            }

            var datos = new Dictionary<string, string>
            {
                ["PacienteNombre"] = $"{turno.Paciente.Nombre} {turno.Paciente.Apellido}",
                ["ProfesionalNombre"] = turno.Profesional != null ? $"{turno.Profesional.Nombre} {turno.Profesional.Apellido}" : "Médico Asignado",
                ["Especialidad"] = turno.Profesional?.Especialidad?.Nombre ?? "Medicina General",
                ["FechaHora"] = turno.FechaHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                ["HorasRestantes"] = _options.HorasAntesDelRecordatorio.ToString()
            };

            var req = new NotificacionRequest
            {
                Destinatario = turno.Paciente.Email,
                Evento = EventoNotificacion.RecordatorioTurno,
                TurnoId = turno.Id,
                Datos = datos
            };

            var exito = await emailService.NotificarAsync(req);
            if (exito) enviados++;
        }

        return enviados;
    }
}
