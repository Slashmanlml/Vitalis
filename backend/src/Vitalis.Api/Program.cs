using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vitalis.Infrastructure;
using Vitalis.Infrastructure.Data;
using Serilog;
using Serilog.Events;
using Vitalis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Vitalis.Api.Middleware;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Vitalis.Api.Controllers;

// El frontend es la app Angular en vitalis-frontend/. En desarrollo corre aparte
// con `ng serve` y consume esta API por HTTP con CORS. En Docker lo sirve nginx,
// que ademas hace de puente hacia /api, de modo que ambos quedan bajo el mismo
// origen y CORS no interviene. Esta API si sirve archivos estaticos, pero solo
// los adjuntos subidos (wwwroot/uploads).
var builder = WebApplication.CreateBuilder(args);

var esDesarrollo = builder.Environment.IsDevelopment();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    // EF Core registra CADA sentencia SQL en nivel Information. Mientras se
    // desarrolla es util para ver que consulta genero, pero en produccion
    // arrancar el sistema imprimia cientos de lineas con toda la siembra, y un
    // error real quedaba enterrado ahi adentro. En produccion solo se muestran
    // los problemas de base de datos, no la actividad normal.
    .MinimumLevel.Override(
        "Microsoft.EntityFrameworkCore.Database.Command",
        esDesarrollo ? LogEventLevel.Information : LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vitalis API",
        Version = "v1",
        Description = "API del sistema de gestión para consultorio médico virtual."
    });

    // 🔐 Configuración de seguridad para JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer. Ejemplo: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");

// La clave que firma los tokens NO puede vivir en el repositorio: antes estaba
// escrita en appsettings.json, y cualquiera con acceso al código podía fabricar
// un token de Administrador válido. Ahora se lee de la configuración local
// (appsettings.Development.json, que no se sube a git), de user-secrets o de la
// variable de entorno Jwt__Key. Si falta o es corta, la API no arranca: es
// preferible no levantar a levantar sin autenticación (antes, sin clave, se
// salteaba en silencio el registro de JwtBearer).
var claveJwt = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(claveJwt) || Encoding.UTF8.GetByteCount(claveJwt) < 32)
{
    throw new InvalidOperationException(
        "Falta la clave de firma JWT (Jwt:Key) o tiene menos de 32 bytes. " +
        "Configurala en appsettings.Development.json, con 'dotnet user-secrets set Jwt:Key ...' " +
        "o con la variable de entorno Jwt__Key. Ver backend/README.md.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            // Por defecto .NET tolera 5 minutos de diferencia de reloj después
            // del vencimiento. Con un solo servidor no hace falta tanto.
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>()
                    .LogWarning(context.Exception, "Token JWT inválido.");
                return Task.CompletedTask;
            }
        };
    });

// Límite de pedidos por IP en los endpoints públicos. Complementa el bloqueo
// por cuenta (AuthService): el bloqueo frena a quien prueba muchas contraseñas
// contra UN usuario; esto frena a quien prueba pocas contra MUCHOS usuarios
// (password spraying) y a quien usa el registro para mandar correos en masa.
var limites = builder.Configuration.GetSection("LimitePedidos");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"mensaje\":\"Demasiados intentos seguidos. Esperá un momento y volvé a probar.\",\"codigo\":\"DEMASIADOS_PEDIDOS\"}",
            token);
    };

    options.AddPolicy(AuthController.PoliticaLogin, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limites.GetValue("LoginPorMinuto", 10),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(AuthController.PoliticaRegistro, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limites.GetValue("RegistroCada10Minutos", 10),
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
});

// Detrás de nginx (Docker) la IP que ve la API es la del proxy, y todos los
// usuarios compartirían el mismo límite. Solo se confía en X-Forwarded-For si
// se habilita explícitamente: si la API quedara expuesta sin proxy, cualquiera
// podría mandar ese encabezado para saltearse el límite.
if (builder.Configuration.GetValue("Proxy:ConfiarEnEncabezados", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddAuthorization();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Configuration.GetValue("Proxy:ConfiarEnEncabezados", false))
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<CabecerasSeguridadMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VitalisDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    // Apply pending migrations only for relational databases
    if (context.Database.IsRelational())
    {
        context.Database.Migrate();
    }
    // Los usuarios de prueba (admin@vitalis.local / Admin123!, etc.) y los datos
    // de demostración solo se cargan si se pide. Antes se cargaban siempre,
    // también en producción: cualquiera que leyera el README entraba como
    // Administrador. Por defecto: sí en Development, no en otro entorno.
    var datosDemo = app.Configuration.GetValue("Semilla:DatosDemo", app.Environment.IsDevelopment());
    await DbSeeder.SeedAsync(context, logger, datosDemo, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Necesario para servir /uploads/* (imágenes subidas por UploadsController) desde wwwroot.
app.UseStaticFiles();

app.Run();

public partial class Program { }
