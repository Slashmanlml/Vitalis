using Microsoft.EntityFrameworkCore;
using Vitalis.Application.Interfaces;
using Vitalis.Domain.Entities;
using Vitalis.Infrastructure.Data;

namespace Vitalis.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly VitalisDbContext _context;

    public EmailService(VitalisDbContext context)
    {
        _context = context;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var log = new EmailLog
        {
            Destinatario = to,
            Asunto = subject,
            Cuerpo = body,
            FechaEnvio = DateTime.UtcNow
        };

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();
        
        // Log to console/Serilog for reference
        Console.WriteLine($"[EMAIL SIMULADO] Destinatario: {to} | Asunto: {subject}");
    }

    public async Task<IEnumerable<EmailLog>> GetEmailLogsAsync()
    {
        return await _context.EmailLogs
            .OrderByDescending(e => e.FechaEnvio)
            .ToListAsync();
    }
}
