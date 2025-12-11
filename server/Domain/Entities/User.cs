using Domain.Common;
using System.Text.RegularExpressions;


using Domain.Events.UserEvents;

namespace Domain.Entities;

public class User : Entity
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    // === Owned Entities ===
    private readonly List<Session> _sessions = new();
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    private User() { } // EF Core

    private User(Guid id, string name, string email, string passwordHash)
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

    public Session Login(string refreshToken, DateTimeOffset expiresAt, string userAgent, string ipAddress)
    {
        var session = Session.Create(Id, refreshToken, expiresAt, userAgent, ipAddress);
        _sessions.Add(session);
        return session;
    }

    public void Logout(string refreshToken, DateTimeOffset? revokedAt = null)
    {
        var session = _sessions.FirstOrDefault(s => s.RefreshToken == refreshToken) ?? throw new InvalidOperationException("Session not found.");
        session.Revoke(revokedAt);
    }

    public void LogoutAllSessions(DateTimeOffset? revokedAt = null)
    {
        foreach (var session in _sessions.Where(s => !s.RevokedAt.HasValue))
        {
            session.Revoke(revokedAt);
        }
    }

    // === Session Management ===

    public void RevokeSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId) ?? throw new InvalidOperationException("Session not found.");
        session.Revoke();
    }

    public void ExtendSession(string refreshToken, TimeSpan duration)
    {
        var session = _sessions.FirstOrDefault(s => s.RefreshToken == refreshToken) ?? throw new InvalidOperationException("Session not found.");
        session.ExtendExpiration(duration);
    }

    // === User Updates ===
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