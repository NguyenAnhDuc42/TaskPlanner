namespace Application;

public record PagedResult<T>(
    IEnumerable<T> Items,
    string? NextCursor,
    bool HasNextPage);


//cursor pagi stuff 
//here
//CursorPaginationRequest
//CursorHelper
