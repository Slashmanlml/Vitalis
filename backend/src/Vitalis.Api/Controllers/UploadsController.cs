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
[Authorize]
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
}
