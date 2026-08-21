using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitalis.Application.Interfaces;
using Vitalis.Infrastructure.Data;
using Vitalis.Infrastructure.Services;

namespace Vitalis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VitalisDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPacienteService, PacienteService>();
        services.AddScoped<IObraSocialService, ObraSocialService>();
        services.AddScoped<IEspecialidadService, EspecialidadService>();
        services.AddScoped<IProfesionalService, ProfesionalService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<ITurnoService, TurnoService>();
        services.AddScoped<IReporteService, ReporteService>();
        services.AddScoped<IConsultaMedicaService, ConsultaMedicaService>();
        services.AddScoped<IMedicamentoService, MedicamentoService>();
        services.AddScoped<IPrescripcionService, PrescripcionService>();
        services.AddScoped<IPrestacionService, PrestacionService>();
        services.AddScoped<IFacturaService, FacturaService>();
        services.AddScoped<ILiquidacionService, LiquidacionService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBloqueoAgendaService, BloqueoAgendaService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
