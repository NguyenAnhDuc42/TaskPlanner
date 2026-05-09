using System;
using Application.Common.Interfaces;
using Domain.Enums.RelationShip;


namespace Application.Features.SpaceFeatures;

public record UpdateSpaceCommand(
    Guid SpaceId,
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate
) : ICommandRequest, IAuthorizedWorkspaceRequest;