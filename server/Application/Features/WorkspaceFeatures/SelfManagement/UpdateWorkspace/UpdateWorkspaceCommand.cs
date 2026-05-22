namespace Application;

public record UpdateWorkspaceCommand(
    Guid Id,
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    Theme? Theme,
    bool? StrictJoin,
    bool? IsArchived,
    bool RegenerateJoinCode
) : ICommandRequest, IAuthorizedWorkspaceRequest;


