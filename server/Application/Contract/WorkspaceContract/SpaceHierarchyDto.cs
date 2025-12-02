namespace Application.Contract.WorkspaceContract;

public record class SpaceHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public bool IsPrivate { get; init; }
    public List<FolderHierarchyDto> Folders { get; init; } = new();
    public List<ListHierarchyDto> Lists { get; init; } = new();
}
