using System.Collections.Generic;
using Application.Common.Interfaces;
using Domain.Enums;
using Application.Features.TaskFeatures.SelfManagement;

namespace Application.Features.ViewFeatures.GetViewData;

public record StatusDto(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);

public record GetViewDataQuery(Guid ViewId) : IQueryRequest<BaseViewResult>;

public abstract record BaseViewResult(ViewType ViewType);

public record TaskListViewResult(
    IEnumerable<TaskDto> Tasks,
    IEnumerable<StatusDto> Statuses
) : BaseViewResult(ViewType.List);

public record TasksBoardViewResult(
    IEnumerable<TaskDto> Tasks,
    IEnumerable<StatusDto> Statuses
) : BaseViewResult(ViewType.Board);