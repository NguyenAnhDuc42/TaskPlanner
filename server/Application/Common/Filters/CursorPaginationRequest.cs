using System;

namespace Application.Common.Filters;

public record CursorPaginationRequest(
    string? Cursor = null,
    int PageSize = 10,
    string SortBy = "UpdatedAt",
    SortDirection Direction = SortDirection.Ascending);

public enum SortDirection { Ascending, Descending }
