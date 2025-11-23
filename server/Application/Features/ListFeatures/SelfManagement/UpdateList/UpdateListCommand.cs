using System;
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
    List<Guid>? MemberIdsToAdd
) : IRequest<Unit>;
