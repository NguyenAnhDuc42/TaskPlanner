namespace Application.Contract.WorkspaceContract;

public record class ListHierarchyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public bool IsPrivate { get; init; }
}
