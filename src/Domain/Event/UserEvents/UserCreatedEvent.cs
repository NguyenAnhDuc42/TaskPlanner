using System;
using src.Domain.Entities.UserEntity;

namespace src.Domain.Event.UserEvents;

public class UserCreatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string Name { get; }

    public UserCreatedEvent(Guid userId, string email, string name)
    {
        UserId = userId;
        Email = email;
        Name = name;
    }
}