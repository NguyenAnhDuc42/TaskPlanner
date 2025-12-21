using Domain.Common;
using System.Text.RegularExpressions;
using Domain.Events.UserEvents;

namespace Domain.Entities;

public class User : Entity
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? PasswordHash { get; private set; }
    public string? AuthProvider { get; private set; }
    public string? ExternalId { get; private set; }

    private User() { } // EF Core

    private User(Guid id, string name, string email, string? passwordHash)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static User Create(string name, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format.", nameof(email));

        var user = new User(Guid.NewGuid(), name, email, passwordHash);
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.Name, DateTimeOffset.UtcNow));
        return user;
    }

    public static User CreateExternal(string name, string email, string provider, string externalId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required", nameof(email));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider required", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalId)) throw new ArgumentException("ExternalId required", nameof(externalId));

        var user = new User(Guid.NewGuid(), name, email, null)
        {
            AuthProvider = provider,
            ExternalId = externalId
        };
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Email, user.Name, DateTimeOffset.UtcNow));
        return user;
    }

    public void LinkExternalAccount(string provider, string externalId)
    {
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider required", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalId)) throw new ArgumentException("ExternalId required", nameof(externalId));

        AuthProvider = provider;
        ExternalId = externalId;
    }

    public bool IsLinkedToProvider(string provider) => AuthProvider == provider;

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("User name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
    }

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email cannot be empty.", nameof(newEmail));
        if (!IsValidEmail(newEmail))
            throw new ArgumentException("Invalid email format.", nameof(newEmail));
        if (Email == newEmail) return;

        Email = newEmail;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password cannot be empty.", nameof(newPasswordHash));
        if (PasswordHash == newPasswordHash) return;

        PasswordHash = newPasswordHash;
    }

    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}