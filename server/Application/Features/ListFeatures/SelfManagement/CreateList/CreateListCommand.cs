using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ListFeatures.SelfManagement.CreateList;

public record class CreateListCommand(
    Guid spaceId,
    Guid? folderId,
    string name,
    string color,
    string icon,
    bool isPrivate,
    List<Guid>? memberIdsToInvite = null
) : ICommand<Guid>;
