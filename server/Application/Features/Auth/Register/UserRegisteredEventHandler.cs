using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Domain.Events.UserEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Register;

public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UserRegisteredEvent for User {UserId}. Welcome email disabled.", notification.UserId);
        
        // Email sending removed as per request.
        return Task.CompletedTask;
    }
}
