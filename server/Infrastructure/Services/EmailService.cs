using System;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        // STUB: Real implementation would use SMTP/SendGrid
        _logger.LogInformation("Sending email to {To}: {Subject}", to, subject);
        _logger.LogDebug("Email Body: {Body}", body);
        
        return Task.CompletedTask;
    }
}
