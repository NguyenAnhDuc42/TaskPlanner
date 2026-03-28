namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public record WorkspaceSecurityContextDto
{
    public Guid WorkspaceId { get; init; }
    public string CurrentRole { get; init; } = string.Empty;
    public bool IsOwned { get; init; }
    public Domain.Enums.Workspace.Theme Theme { get; init; }
    public string Color { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
}

