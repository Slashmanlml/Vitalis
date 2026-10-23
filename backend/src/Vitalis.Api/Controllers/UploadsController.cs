using Vitalis.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.PersonalClinica)]
public class UploadsController : ControllerBase
{
    private const long TamanioMaximoBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _env;

    public UploadsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    [RequestSizeLimit(TamanioMaximoBytes)]
    public async Task<IActionResult> SubirImagen(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { mensaje = "No se ha enviado ningún archivo." });
        }

        if (file.Length > TamanioMaximoBytes)
        {
            return BadRequest(new { mensaje = "El archivo no puede superar los 5 MB." });
        }

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(extension))
        {
            return BadRequest(new { mensaje = "El archivo enviado no es una imagen válida (.jpg, .jpeg, .png, .webp)." });
        }

        // La extensión la elige quien sube el archivo, así que no prueba nada:
        // un .exe renombrado a .png pasaría. Se mira además la "firma" del
        // archivo (los primeros bytes), que corresponde al formato real.
        if (!await TieneFirmaDeImagenAsync(file, extension))
        {
            return BadRequest(new { mensaje = "El contenido del archivo no corresponde a una imagen válida." });
        }

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(webRoot, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var nombreUnico = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, nombreUnico);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{nombreUnico}";
        return Ok(new { url = relativeUrl });
    }

    private static async Task<bool> TieneFirmaDeImagenAsync(IFormFile file, string extension)
    {
        var cabecera = new byte[12];
        await using var stream = file.OpenReadStream();
        var leidos = await stream.ReadAsync(cabecera.AsMemory(0, cabecera.Length));
        if (leidos < 4) return false;

        return extension switch
        {
            ".jpg" or ".jpeg" => cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF,
            ".png" => cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47,
            // RIFF....WEBP
            ".webp" => leidos >= 12
                       && cabecera[0] == 0x52 && cabecera[1] == 0x49 && cabecera[2] == 0x46 && cabecera[3] == 0x46
                       && cabecera[8] == 0x57 && cabecera[9] == 0x45 && cabecera[10] == 0x42 && cabecera[11] == 0x50,
            _ => false
        };
    }
}
