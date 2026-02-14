using System;
using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.ListFeatures.SelfManagement.UpdateList;

public record UpdateListCommand(
    Guid ListId,
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    List<UpdateListMemberValue>? MembersToAddOrUpdate
) : ICommand<Unit>;

public record UpdateListMemberValue(Guid workspaceMemberId, AccessLevel? accessLevel, bool isRemove = false);
