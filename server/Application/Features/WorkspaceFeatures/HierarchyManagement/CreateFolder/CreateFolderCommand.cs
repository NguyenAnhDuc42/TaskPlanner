using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateFolder;

public record CreateFolderCommand(
    Guid spaceId,
    string name,
    string? color,
    string? icon,
    bool isPrivate,
    List<Guid>? memberIdsToInvite  // Invite members immediately on creation
) : IRequest<Guid>;  // Return folder ID for navigation