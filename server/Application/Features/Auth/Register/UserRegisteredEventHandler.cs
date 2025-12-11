using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Domain.Events.UserEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Register;

public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(IEmailService emailService, ILogger<UserRegisteredEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UserRegisteredEvent for User {UserId}", notification.UserId);

        var subject = $"Welcome to TaskPlanner, {notification.Username}!";
        var body = $"Hi {notification.Username},\n\nThanks for registering. Please verify your email: {notification.Email}.";

        await _emailService.SendEmailAsync(notification.Email, subject, body);
        
        _logger.LogInformation("Welcome email sent to {Email}", notification.Email);
    }
}
