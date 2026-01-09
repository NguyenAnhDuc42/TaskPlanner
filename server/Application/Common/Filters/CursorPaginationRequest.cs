using System;

namespace Application.Common.Filters;

public record CursorPaginationRequest(
    string? Cursor = null,
    int PageSize = 8,
    string SortBy = "UpdatedAt",
    SortDirection Direction = SortDirection.Ascending);

public enum SortDirection { Ascending, Descending }
