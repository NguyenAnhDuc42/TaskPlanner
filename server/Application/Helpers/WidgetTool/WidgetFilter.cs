using Domain.Enums;

namespace Application.Helpers.WidgetTool;


public record WidgetFilter
{
    public string? SearchText { get; init; }
    public List<Guid> TagIds { get; init; } = new();
    public List<Guid> StatusIds { get; init; } = new();
    public List<Priority> PriorityIds { get; init; } = new();
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public int? Limit { get; init; }
}