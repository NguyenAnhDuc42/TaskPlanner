using Domain.Common;

namespace Domain.Entities;

public class Workflow : TenantEntity
{
    public Guid? ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private readonly List<Status> _statuses = new();
    public virtual IReadOnlyCollection<Status> Statuses => _statuses.AsReadOnly();

    private Workflow() { }

    private Workflow(Guid id, Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string description, Guid creatorId) 
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name;
        Description = description;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static Workflow Create(Guid projectWorkspaceId, string name, string description, Guid creatorId, Guid? projectSpaceId = null, Guid? projectFolderId = null)
    {
        return new Workflow(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, projectFolderId, name, description, creatorId);
    }

    public Workflow Clone(Guid creatorId)
    {
        var newWorkflowId = Guid.NewGuid();
        var cloned = new Workflow(newWorkflowId, ProjectWorkspaceId, ProjectSpaceId, ProjectFolderId, $"{Name} (Copy)", Description, creatorId);
        
        foreach (var status in Statuses)
        {
            var newStatus = Status.Create(ProjectWorkspaceId, newWorkflowId, status.Name, status.Color, status.Category, creatorId);
            cloned.AddStatus(newStatus);
        }

        return cloned;
    }

    public void AddStatus(Status status)
    {
        if (status == null) throw new ArgumentNullException(nameof(status));
        if (status.WorkflowId != Id) status.SetWorkflow(Id);
        
        if (!_statuses.Any(s => s.Id == status.Id))
        {
            _statuses.Add(status);
            UpdateTimestamp();
        }
    }

    public void RemoveStatus(Guid statusId)
    {
        var status = _statuses.FirstOrDefault(s => s.Id == statusId);
        if (status != null)
        {
            _statuses.Remove(status);
            UpdateTimestamp();
        }
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void SetOwner(Guid? projectSpaceId, Guid? projectFolderId)
    {
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        UpdateTimestamp();
    }

    public void ValidateIntegrity()
    {
        var categories = Statuses.Select(s => s.Category).Distinct().ToList();
        var requiredCategories = Enum.GetValues<Domain.Enums.StatusCategory>();

        foreach (var category in requiredCategories)
        {
            if (!categories.Contains(category))
            {
                throw new Domain.Exceptions.BusinessRuleException($"Workflow '{Name}' must have at least one status in the '{category}' category.");
            }
        }
    }
}
