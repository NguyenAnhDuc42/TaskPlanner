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
    public string OrderKey { get; private set; } = null!;

    private Status() { } // EF Core

    private Status(Guid id, Guid projectWorkspaceId, Guid workflowId, string name, string color, StatusCategory category, Guid creatorId, string orderKey)
        : base(id, projectWorkspaceId)
    {
        WorkflowId = workflowId;
        Name = name;
        Color = color;
        Category = category;
        OrderKey = orderKey;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static Status Create(Guid projectWorkspaceId, Guid workflowId, string name, string color, StatusCategory category, Guid creatorId, string? orderKey = null)
        => new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, name, color, category, creatorId, orderKey ?? FractionalIndex.Start());

    public static List<Status> CreateStarterSet(Guid projectWorkspaceId, Guid workflowId, Guid creatorId)
    {
        var start = FractionalIndex.Start();
        return new List<Status>
        {
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "To Do", "#808080", StatusCategory.NotStarted, creatorId, start),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "In Progress", "#1e90ff", StatusCategory.Active, creatorId, FractionalIndex.After(start)),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Complete", "#008000", StatusCategory.Done, creatorId, FractionalIndex.After(FractionalIndex.After(start)))
        };
    }

    public static List<Status> CreateSpaceStarterSet(Guid projectWorkspaceId, Guid workflowId, Guid creatorId)
    {
        var start = FractionalIndex.Start();
        var key2 = FractionalIndex.After(start);
        var key3 = FractionalIndex.After(key2);
        var key4 = FractionalIndex.After(key3);

        return new List<Status>
        {
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Planned", "#808080", StatusCategory.NotStarted, creatorId, start),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "In Progress", "#1e90ff", StatusCategory.Active, creatorId, key2),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Paused", "#ff8c00", StatusCategory.Active, creatorId, key3),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Completed", "#008000", StatusCategory.Done, creatorId, key4)
        };
    }

    public static List<Status> CreateFolderStarterSet(Guid projectWorkspaceId, Guid workflowId, Guid creatorId)
    {
        var start = FractionalIndex.Start();
        var key2 = FractionalIndex.After(start);
        var key3 = FractionalIndex.After(key2);
        var key4 = FractionalIndex.After(key3);

        return new List<Status>
        {
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Backlog", "#a9a9a9", StatusCategory.NotStarted, creatorId, start),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Todo", "#808080", StatusCategory.NotStarted, creatorId, key2),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "In Progress", "#1e90ff", StatusCategory.Active, creatorId, key3),
            new Status(Guid.NewGuid(), projectWorkspaceId, workflowId, "Done", "#008000", StatusCategory.Done, creatorId, key4)
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

    public void UpdateOrderKey(string orderKey)
    {
        if (OrderKey == orderKey) return;
        OrderKey = orderKey;
        UpdateTimestamp();
    }

    public void SetWorkflow(Guid workflowId)
    {
        if (WorkflowId == workflowId) return;
        WorkflowId = workflowId;
        UpdateTimestamp();
    }
}
