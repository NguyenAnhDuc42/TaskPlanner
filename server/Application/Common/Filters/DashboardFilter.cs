namespace Application.Common.Filters;

public record class DashboardFilter(string? name = null, bool? owned = false, DateTimeOffset? createdAt = null);
