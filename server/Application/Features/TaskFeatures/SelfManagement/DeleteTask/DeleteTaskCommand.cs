using Application.Common.Interfaces;
using System;
using MediatR;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public record DeleteTaskCommand(Guid TaskId) : ICommand<Unit>;
