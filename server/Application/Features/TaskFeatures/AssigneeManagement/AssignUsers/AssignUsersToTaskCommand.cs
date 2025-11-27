using System;
using MediatR;

namespace Application.Features.TaskFeatures.AssigneeManagement.AssignUsers;

public record AssignUsersToTaskCommand(
    Guid TaskId,
    List<Guid> UserIds
) : IRequest<Unit>;
