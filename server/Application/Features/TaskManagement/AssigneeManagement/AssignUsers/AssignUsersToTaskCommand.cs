using System;
using MediatR;

namespace Application.Features.TaskManagement.AssigneeManagement.AssignUsers;

public record AssignUsersToTaskCommand(
    Guid TaskId,
    List<Guid> UserIds
) : IRequest<Unit>;
