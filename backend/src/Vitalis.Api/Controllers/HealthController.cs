using Microsoft.AspNetCore.Mvc;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            sistema = "Vitalis",
            estado = "ok",
            fecha = DateTime.UtcNow
        });
}
