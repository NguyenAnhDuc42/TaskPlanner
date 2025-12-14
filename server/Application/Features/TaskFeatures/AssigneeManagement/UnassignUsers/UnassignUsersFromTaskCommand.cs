using System;
using MediatR;

namespace Application.Features.TaskFeatures.AssigneeManagement.UnassignUsers;

public record UnassignUsersFromTaskCommand(
    Guid TaskId,
    List<Guid> UserIds
) : IRequest<Unit>;
