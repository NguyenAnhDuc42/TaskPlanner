using Application.Common.Interfaces;
using Application.Contract.Common;
using Application.Contract.StatusContract;
using Domain.Enums;

namespace Application.Features.ViewFeatures.GetViewData;

public record GetViewDataQuery(Guid ViewId) : IQuery<BaseViewResult>;


public abstract record BaseViewResult(ViewType ViewType);

public record TaskListViewResult(
    IEnumerable<TaskDto> Tasks,
    IEnumerable<StatusDto> Statuses
) : BaseViewResult(ViewType.List);

public record TasksBoardViewResult(
    IEnumerable<TaskDto> Tasks,
    IEnumerable<StatusDto> Statuses
) : BaseViewResult(ViewType.Board);

public record DocumentListResult(
    IEnumerable<DocumentDto> Documents,
    IEnumerable<StatusDto> Statuses
) : BaseViewResult(ViewType.Doc);