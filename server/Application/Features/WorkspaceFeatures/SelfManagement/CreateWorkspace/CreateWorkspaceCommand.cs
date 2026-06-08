namespace Application;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    string Color,
    string Icon,
    Theme Theme,
    bool StrictJoin
) : ICommandRequest<Guid>;

