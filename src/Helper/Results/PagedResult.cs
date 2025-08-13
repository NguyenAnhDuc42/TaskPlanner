namespace src.Helper.Results;

public record PagedResult<T>(T Data, string? NextCursor, bool HasNextPage);
