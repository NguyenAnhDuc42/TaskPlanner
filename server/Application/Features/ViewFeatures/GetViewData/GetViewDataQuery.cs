using System.Collections.Generic;
using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.ViewFeatures.GetViewData;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid ProjectWorkspaceId { get; set; }
    public Guid? ProjectSpaceId { get; set; }
    public Guid? ProjectFolderId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? StatusId { get; set; }
    public Priority Priority { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int? StoryPoints { get; set; }
    public long? TimeEstimate { get; set; }
    public string? OrderKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<AssigneeDto> Assignees { get; set; } = new();
}

public record AssigneeDto(
    Guid Id,
    string Name,
    string? AvatarUrl
);

public record StatusDto(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);

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