using Domain.Enums;
using Domain.Enums.Workspace;

namespace Application.Contract.WorkspaceContract;

public record class WorkspaceSettingsDto
{
    public Theme Theme { get; init; }
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public bool StrictJoin { get; init; }
}
