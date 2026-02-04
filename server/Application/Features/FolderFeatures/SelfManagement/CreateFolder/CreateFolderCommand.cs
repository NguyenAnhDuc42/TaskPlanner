using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.FolderFeatures.SelfManagement.CreateFolder;

public record class CreateFolderCommand(
    Guid spaceId,
    string name,
    string color,
    string icon,
    bool isPrivate,
    List<Guid>? memberIdsToInvite = null
) : ICommand<Guid>;