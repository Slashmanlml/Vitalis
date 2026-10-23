namespace Vitalis.Api.Middleware;

/// <summary>
/// Cabeceras HTTP de seguridad en todas las respuestas de la API.
///
/// La API solo devuelve JSON e imágenes subidas, nunca páginas HTML, así que
/// la política puede ser la más estricta: nada se ejecuta, nada se embebe.
/// Las cabeceras del frontend (la SPA de Angular) las pone nginx, ver
/// vitalis-frontend/nginx.conf.
/// </summary>
public class CabecerasSeguridadMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            // Que el navegador no "adivine" el tipo: una imagen subida no puede
            // terminar interpretándose como HTML o JavaScript.
            h["X-Content-Type-Options"] = "nosniff";
            // Nadie puede meter la API en un iframe (clickjacking).
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "no-referrer";
            h["Cross-Origin-Resource-Policy"] = "same-site";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Swagger UI sí es una página con scripts: se le deja su política
            // por defecto (solo existe en Development).
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                h["Content-Security-Policy"] = "default-src 'none'; img-src 'self'; frame-ancestors 'none'";
            }

            // Respuestas con datos de salud: que no queden en cachés intermedias.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                h["Cache-Control"] = "no-store";
            }
            return Task.CompletedTask;
        });

        return next(context);
    }
}
