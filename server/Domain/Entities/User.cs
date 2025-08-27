using Domain.Common;
using Domain.Events.UserEvents;
using Domain.Entities.Relationship;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Enums;

namespace Domain.Entities;

public class User : Aggregate
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    // === Owned Entities ===
    private readonly List<Session> _sessions = new();
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    // === Relationship Entities ===
    private readonly List<UserProjectWorkspace> _workspaces = new();
    public IReadOnlyCollection<UserProjectWorkspace> Workspaces => _workspaces.AsReadOnly();

    private readonly List<UserProjectSpace> _spaces = new();
    public IReadOnlyCollection<UserProjectSpace> Spaces => _spaces.AsReadOnly();

    private readonly List<UserProjectFolder> _folders = new();
    public IReadOnlyCollection<UserProjectFolder> Folders => _folders.AsReadOnly();

    private readonly List<UserProjectList> _lists = new();
    public IReadOnlyCollection<UserProjectList> Lists => _lists.AsReadOnly();

    private readonly List<UserProjectTask> _tasks = new();
    public IReadOnlyCollection<UserProjectTask> Tasks => _tasks.AsReadOnly();

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
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Name, user.Email));
        return user;
    }

    // === Session Management ===
    public Session AddSession(string refreshToken, DateTime expiresAt, string userAgent, string ipAddress)
    {
        var session = Session.Create(Id, refreshToken, expiresAt, userAgent, ipAddress);
        _sessions.Add(session);
        return session;
    }

    public void RevokeSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId)
                      ?? throw new InvalidOperationException("Session not found.");
        session.Revoke();
    }

    public void ExtendSession(Guid sessionId, TimeSpan duration)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId)
                      ?? throw new InvalidOperationException("Session not found.");
        session.ExtendExpiration(duration);
    }

    // === Relationship Management ===
    public void JoinWorkspace(Guid workspaceId, Role role)
    {
        if (_workspaces.Any(w => w.ProjectWorkspaceId == workspaceId)) return;

        var membership = UserProjectWorkspace.Create(Id, workspaceId, role);
        _workspaces.Add(membership);
        UpdateTimestamp();
    }

    public void LeaveWorkspace(Guid workspaceId)
    {
        var membership = _workspaces.FirstOrDefault(w => w.ProjectWorkspaceId == workspaceId);
        if (membership != null) _workspaces.Remove(membership);
    }

    public void JoinSpace(Guid spaceId)
    {
        if (_spaces.Any(s => s.ProjectSpaceId == spaceId)) return;

        var membership = UserProjectSpace.Create(Id, spaceId);
        _spaces.Add(membership);
        UpdateTimestamp();
    }

    public void JoinFolder(Guid folderId)
    {
        if (_folders.Any(f => f.ProjectFolderId == folderId)) return;

        var membership = UserProjectFolder.Create(Id, folderId);
        _folders.Add(membership);
        UpdateTimestamp();
    }

    public void JoinList(Guid listId)
    {
        if (_lists.Any(l => l.ProjectListId == listId)) return;

        var membership = UserProjectList.Create(Id, listId);
        _lists.Add(membership);
        UpdateTimestamp();
    }

    public void AssignToTask(Guid taskId)
    {
        if (_tasks.Any(t => t.ProjectTaskId == taskId)) return;

        var assignment = UserProjectTask.Create(Id, taskId);
        _tasks.Add(assignment);
        UpdateTimestamp();
    }

    // === User Updates ===
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("User name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        AddDomainEvent(new UserNameUpdatedEvent(Id, newName));
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
        if (PasswordHash == newPasswordHash) return;

        PasswordHash = newPasswordHash;
        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}
