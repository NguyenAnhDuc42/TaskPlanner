using Application.Common.Interfaces;
using System;
using Domain.Enums;
using MediatR;

namespace Application.Features.TaskFeatures.SelfManagement.UpdateTask;

public record UpdateTaskCommand(
    Guid TaskId,
    string? Name,
    string? Description,
    Guid? StatusId,
    Priority? Priority,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate
) : ICommand<Unit>;
