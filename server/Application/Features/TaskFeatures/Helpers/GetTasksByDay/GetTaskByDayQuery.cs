using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByDay;

public record GetTaskByDayQuery(DateTime day) : IQuery<Unit>;
