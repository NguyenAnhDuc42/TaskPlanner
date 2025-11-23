using System;
using MediatR;

namespace Application.Features.TaskManagement.SelfManagement.DeleteTask;

public record DeleteTaskCommand(Guid TaskId) : IRequest<Unit>;
