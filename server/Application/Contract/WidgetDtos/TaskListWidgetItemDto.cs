using Domain.Enums;

namespace Application.Contract.WidgetDtos;

public record class TaskListWidgetItemDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public Guid StatusId { get; set; }

    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public Priority Priority { get; set; }
}
