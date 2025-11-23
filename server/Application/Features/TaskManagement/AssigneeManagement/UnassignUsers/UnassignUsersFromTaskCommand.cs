using System;
using MediatR;

namespace Application.Features.TaskManagement.AssigneeManagement.UnassignUsers;

public record UnassignUsersFromTaskCommand(
    Guid TaskId,
    List<Guid> UserIds
) : IRequest<Unit>;
