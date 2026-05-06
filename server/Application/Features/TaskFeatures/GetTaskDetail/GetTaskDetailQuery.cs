using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.TaskFeatures;

public record GetTaskDetailQuery(Guid TaskId) : IQueryRequest<TaskDetailDto>, IAuthorizedWorkspaceRequest;

public record TaskDetailDto
{
    public Guid Id { get; init; }
    public Guid? ProjectSpaceId { get; init; }
    public Guid? ProjectFolderId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Color { get; init; } = null!;
    public string? Icon { get; init; }
    public Guid? StatusId { get; init; }
    public bool IsArchived { get; init; }
    public Priority Priority { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public int? StoryPoints { get; init; }
    public long? TimeEstimateSeconds { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    
    // Pointers for local-first dictionary mapping
    public List<Guid> AssigneeIds { get; init; } = new();
}
