using Domain.Common;
using Domain.Events.UserEvents;
using System;
using System.Collections.Generic;

namespace Domain.Entities;

public class User : Aggregate
{
    // Public Properties
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    // Navigation Properties
    private readonly List<Session> _sessions = new();
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    // Constructors
    private User() { } // For EF Core

    private User(Guid id, string name, string email, string passwordHash)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
    }

    // Static Factory Methods
    public static User Create(string name, string email, string passwordHash)
    {
        // Enforce invariants
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        var user = new User(Guid.NewGuid(), name, email, passwordHash);
        
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Name, user.Email));
        
        return user;
    }

    // Public Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("User name cannot be empty.", nameof(newName));

        if (Name == newName) return;

        Name = newName;
        // Add a future UserProfileUpdatedEvent if needed
    }
}