using Domain.Enums;

namespace Domain.Entities;

public record ViewFilterConfig
{
    public List<Guid> StatusIds { get; init; } = new();
    public List<Priority> Priorities { get; init; } = new();
    public List<Guid> AssigneeIds { get; init; } = new();
    public string? SearchQuery { get; init; }
    public DateTimeOffset? StartDateAfter { get; init; }
    public DateTimeOffset? DueDateBefore { get; init; }

    public static ViewFilterConfig CreateDefault() => new();
}
