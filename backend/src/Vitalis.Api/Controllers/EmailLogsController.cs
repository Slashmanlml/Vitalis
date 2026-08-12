using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Constants;

namespace Vitalis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrador)]
public class EmailLogsController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailLogsController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var logs = await _emailService.GetEmailLogsAsync();
        return Ok(logs);
    }
}
