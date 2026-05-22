using System;

namespace Application;

public record CursorPaginationRequest(
    string? Cursor = null,
    int PageSize = 8,
    string SortBy = "UpdatedAt",
    SortDirection Direction = SortDirection.Ascending);

public enum SortDirection { Ascending, Descending }

