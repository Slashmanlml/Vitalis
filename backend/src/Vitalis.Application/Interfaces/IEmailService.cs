namespace Vitalis.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task<IEnumerable<Domain.Entities.EmailLog>> GetEmailLogsAsync();
}
