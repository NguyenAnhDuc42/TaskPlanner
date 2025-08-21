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
    public Guid CreatorId { get; private set; }

    // Navigation Properties
    private readonly List<ProjectSpace> _spaces = new();
    public IReadOnlyCollection<ProjectSpace> Spaces => _spaces.AsReadOnly();

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

    public ProjectSpace AddSpace(string name, string icon, string color, Guid creatorId)
    {
        var space = ProjectSpace.Create(name, icon, color, Id, creatorId);
        _spaces.Add(space);
        AddDomainEvent(new SpaceAddedToWorkspaceEvent(Id, space.Id, space.Name));
        return space;
    }

    public void AddMember(Guid userId, Role role)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this workspace.");

        _members.Add(new UserProjectWorkspace { UserId = userId, ProjectWorkspaceId = Id, Role = role });
        AddDomainEvent(new MemberAddedToWorkspaceEvent(Id, userId, role));
    }

    public Status AddDefaultStatus(string name, string color, bool isDoneStatus)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));

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