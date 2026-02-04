using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.SpaceFeatures.SelfManagement.CreateSpace;

public record class CreateSpaceCommand(
    Guid workspaceId,
    string name,
    string? description,
    string color,
    string icon,
    bool isPrivate,
    List<Guid>? memberIdsToInvite = null
) : ICommand<Guid>;