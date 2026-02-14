using System;
using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;

public record UpdateSpaceCommand( Guid SpaceId,
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    List<UpdateSpaceMemberValue>? MembersToAddOrUpdate// Add or update members during update
) : ICommand<Unit>;


public record UpdateSpaceMemberValue (Guid workspaceMemberId, AccessLevel? accessLevel,bool isRemove = false);