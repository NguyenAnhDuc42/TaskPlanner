using Domain.Enums;

namespace Domain.Entities;

public readonly record struct ViewFilterConfig
{
    public List<Guid> StatusIds { get; init; }
    public List<Priority> Priorities { get; init; }
    public List<Guid> AssigneeIds { get; init; }
    public string? SearchQuery { get; init; }
    public DateTimeOffset? StartDateAfter { get; init; }
    public DateTimeOffset? DueDateBefore { get; init; }

    public ViewFilterConfig()
    {
        StatusIds = new();
        Priorities = new();
        AssigneeIds = new();
        SearchQuery = null;
        StartDateAfter = null;
        DueDateBefore = null;
    }

    public static ViewFilterConfig CreateDefault() => new();
}
