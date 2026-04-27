using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities;

public class Status : TenantEntity
{
    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public StatusCategory Category { get; private set; }

    private Status() { } // EF Core

    private Status(Guid id, Guid projectWorkspaceId, Guid workflowId, string name, string color, StatusCategory category, Guid creatorId)
        : base(id, projectWorkspaceId)
    {
        WorkflowId = workflowId;
        Name = name;
        Color = color;
        Category = category;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static Status Create(Guid projectWorkspaceId, Guid workflowId, string name, string color, StatusCategory category, Guid creatorId)
        => new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, name, color, category, creatorId);

    public static List<Status> CreateStarterSet(Guid projectWorkspaceId, Guid workflowId, Guid creatorId)
    {
        return new List<Status>
        {
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "To Do", "#808080", StatusCategory.NotStarted, creatorId),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "In Progress", "#1e90ff", StatusCategory.Active, creatorId),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Complete", "#008000", StatusCategory.Done, creatorId)
        };
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateColor(string color)
    {
        if (Color == color) return;
        Color = color;
        UpdateTimestamp();
    }

    public void UpdateCategory(StatusCategory category)
    {
        if (Category == category) return;
        Category = category;
        UpdateTimestamp();
    }

    public void SetWorkflow(Guid workflowId)
    {
        if (WorkflowId == workflowId) return;
        WorkflowId = workflowId;
        UpdateTimestamp();
    }
}
