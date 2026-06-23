namespace Domain;

// Workflow entity is retained for EF migration history compatibility only.
// Workflows no longer own statuses — statuses now belong directly to spaces via ProjectSpaceId.
public class Workflow : TenantEntity
{
    public Guid? ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private Workflow() { }

    private Workflow(Guid id, Guid projectWorkspaceId, Guid? projectSpaceId, string name, string description, Guid creatorId)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Description = description;
        InitializeAudit(creatorId);
    }

    public static Workflow Create(Guid projectWorkspaceId, string name, string description, Guid creatorId, Guid? projectSpaceId = null)
        => new(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, name, description, creatorId);

    public void SetOwner(Guid? projectSpaceId)
    {
        ProjectSpaceId = projectSpaceId;
        UpdateTimestamp();
    }
}
