using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using Domain.Events.WorkspaceEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectWorkspace;

public class ProjectWorkspace : Aggregate
{
    // Public Properties
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string JoinCode { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public string Icon { get; private set; } = null!;
    
    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; } // New Property
    public Guid CreatorId { get; private set; }

    private readonly List<UserProjectWorkspace> _members = new();
    public IReadOnlyCollection<UserProjectWorkspace> Members => _members.AsReadOnly();

    private readonly List<Status> _defaultStatuses = new();
    public IReadOnlyCollection<Status> DefaultStatuses => _defaultStatuses.AsReadOnly();

    // Constructors
    private ProjectWorkspace() { } // For EF Core

    private ProjectWorkspace(Guid id, string name, string description, string joinCode, string color, string icon, Guid creatorId, bool isPrivate)
    {
        Id = id;
        Name = name;
        Description = description;
        JoinCode = joinCode;
        Color = color;
        Icon = icon;
        CreatorId = creatorId;
        IsPrivate = isPrivate;
        IsArchived = false; // Default
    }

    // Static Factory Methods
    public static ProjectWorkspace Create(string name, string description, string color, string icon, Guid creatorId, bool isPrivate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty.", nameof(name));

        var joinCode = GenerateRandomCode();
        var workspace = new ProjectWorkspace(Guid.NewGuid(), name, description, joinCode, color, icon, creatorId, isPrivate);
        workspace.AddDomainEvent(new WorkspaceCreatedEvent(workspace.Id, workspace.Name, workspace.CreatorId));
        return workspace;
    }

    // Public Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Workspace name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        AddDomainEvent(new WorkspaceNameUpdatedEvent(Id, newName));
    }

    public void UpdateDescription(string newDescription)
    {
        if (Description == newDescription) return;

        Description = newDescription;
        AddDomainEvent(new WorkspaceDescriptionUpdatedEvent(Id, newDescription));
    }

    public void AddSpace(Guid spaceId, string name)
    {
        AddDomainEvent(new SpaceAddedToWorkspaceEvent(Id, spaceId, name));
    }

    public void AddMember(Guid userId, Role role)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this workspace.");

        _members.Add(UserProjectWorkspace.Create(userId, Id, role));
        AddDomainEvent(new MemberAddedToWorkspaceEvent(Id, userId, role));
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null) return;

        _members.Remove(member);
        AddDomainEvent(new MemberRemovedFromWorkspaceEvent(Id, userId));
    }

    public void ChangeMemberRole(Guid userId, Role newRole)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            throw new InvalidOperationException("Member not found in this workspace.");

        member.UpdateRole(newRole);
        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(Id, userId, newRole));
    }

    public void Archive()
    {
        if (IsArchived) return;
        IsArchived = true;
        AddDomainEvent(new WorkspaceArchivedEvent(Id));
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        IsArchived = false;
        AddDomainEvent(new WorkspaceUnarchivedEvent(Id));
    }

    public void GenerateNewJoinCode()
    {
        JoinCode = GenerateRandomCode();
        AddDomainEvent(new WorkspaceJoinCodeChangedEvent(Id, JoinCode));
    }

    public Status AddDefaultStatus(string name, string color, bool isDoneStatus)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));

        if (_defaultStatuses.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A default status with the name '{name}' already exists in this workspace.");

        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Status color cannot be empty.", nameof(color));

        var status = Status.Create(name, color, _defaultStatuses.Count, Id, isDoneStatus);
        _defaultStatuses.Add(status);
        AddDomainEvent(new DefaultStatusAddedToWorkspaceEvent(Id, status.Id, name));
        return status;
    }

    // Helper method for JoinCode
    public static string GenerateRandomCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}