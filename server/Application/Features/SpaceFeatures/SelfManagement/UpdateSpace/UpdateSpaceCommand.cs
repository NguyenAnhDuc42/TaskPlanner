using System;
using MediatR;

namespace Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;

public record UpdateSpaceCommand(
    Guid SpaceId,
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    List<Guid>? MemberIdsToAdd
) : IRequest<Unit>;
