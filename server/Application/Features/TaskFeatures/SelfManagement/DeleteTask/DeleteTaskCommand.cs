using System;
using MediatR;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public record DeleteTaskCommand(Guid TaskId) : IRequest<Unit>;
