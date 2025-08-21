using Domain.Common;
using Domain.Events.UserEvents;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Domain.Entities;

public class User : Aggregate
{
    // Public Properties
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    // Navigation Properties
    

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
        // New: Basic email format validation
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format.", nameof(email));

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
        AddDomainEvent(new UserNameUpdatedEvent(Id, newName)); // Added event
    }

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email cannot be empty.", nameof(newEmail));
        if (!IsValidEmail(newEmail))
            throw new ArgumentException("Invalid email format.", nameof(newEmail));
        if (Email == newEmail) return;

        Email = newEmail;
        AddDomainEvent(new UserEmailUpdatedEvent(Id, newEmail));
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password cannot be empty.", nameof(newPasswordHash));
        // Add more password strength validation here if needed

        if (PasswordHash == newPasswordHash) return;

        PasswordHash = newPasswordHash;
        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    private static bool IsValidEmail(string email)
    {
        // Use a simple regex for email validation.
        // For more robust validation, consider using a dedicated library or a more comprehensive regex.
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}