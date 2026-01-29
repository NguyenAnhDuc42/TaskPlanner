namespace Application.Contract.WorkspaceContract;

public record WorkspaceSecurityContextDto
{
    public Guid WorkspaceId { get; init; }
    public string CurrentRole { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public bool IsOwned { get; init; }
}
