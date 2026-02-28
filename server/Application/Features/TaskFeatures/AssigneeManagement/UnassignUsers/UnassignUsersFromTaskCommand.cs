using Application.Common.Interfaces;
using System;
using MediatR;

namespace Application.Features.TaskFeatures.AssigneeManagement.UnassignUsers;

public record UnassignUsersFromTaskCommand(
    Guid TaskId,
    List<Guid> UserIds
) : ICommand<Unit>;
