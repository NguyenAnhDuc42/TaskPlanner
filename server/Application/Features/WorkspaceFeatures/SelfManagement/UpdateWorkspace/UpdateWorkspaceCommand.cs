using Application.Common.Interfaces;
using Domain.Enums.Workspace;

namespace Application.Features.WorkspaceFeatures.UpdateWorkspace;

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
