namespace Application.Contract.WorkspaceContract;

public record class WorkspaceHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public List<SpaceHierarchyDto> Spaces { get; init; } = new();
}
