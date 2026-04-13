using Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Workflow : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid? SpaceId { get; private set; }
    public Guid? FolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    private readonly List<Status> _statuses = new();
    public virtual IReadOnlyCollection<Status> Statuses => _statuses.AsReadOnly();

    private Workflow() { }

    private Workflow(Guid id, Guid projectWorkspaceId, Guid? spaceId, Guid? folderId, string name, string? description, Guid creatorId) : base(id)
    {
        ProjectWorkspaceId = projectWorkspaceId;
        SpaceId = spaceId;
        FolderId = folderId;
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Description = description?.Trim();
        CreatorId = creatorId;
    }

    public static Workflow Create(Guid projectWorkspaceId, string name, string? description, Guid creatorId, Guid? spaceId = null, Guid? folderId = null)
    {
        return new Workflow(Guid.NewGuid(), projectWorkspaceId, spaceId, folderId, name, description, creatorId);
    }

    public Workflow Clone(Guid creatorId)
    {
        var newWorkflowId = Guid.NewGuid();
        var cloned = new Workflow(newWorkflowId, ProjectWorkspaceId, SpaceId, FolderId, $"{Name} (Copy)", Description, creatorId);
        
        foreach (var status in Statuses)
        {
            var newStatus = Status.Create(ProjectWorkspaceId, newWorkflowId, status.Name, status.Color, status.Category, creatorId);
            cloned._statuses.Add(newStatus);
        }

        return cloned;
    }

    public void UpdateDetails(string name, string? description)
    {
        var candidateName = name.Trim() == string.Empty ? throw new ArgumentException("Name cannot be empty.") : name.Trim();
        var candidateDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        var changed = false;
        if (Name != candidateName) { Name = candidateName; changed = true; }
        if (Description != candidateDescription) { Description = candidateDescription; changed = true; }

        if (changed) UpdateTimestamp();
    }
}
