using Domain.Enums;

namespace Application.Contract.WorkspaceContract;

public record class WorkspaceSummary
{
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Variant { get; init; } = null!;
}