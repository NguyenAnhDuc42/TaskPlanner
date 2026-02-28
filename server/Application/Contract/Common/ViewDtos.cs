using Domain.Enums;

namespace Application.Contract.Common;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid ProjectListId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? StatusId { get; set; }
    public Priority Priority { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int? StoryPoints { get; set; }
    public long? TimeEstimate { get; set; }
    public long? OrderKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record DocumentDto(
    Guid Id,
    Guid LayerId,
    string Name,
    string Content
);
