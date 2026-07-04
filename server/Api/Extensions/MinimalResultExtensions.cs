using Microsoft.AspNetCore.Http;
namespace Api;

/// <summary>
/// Converts Result/Result&lt;T&gt; to Microsoft.AspNetCore.Http.IResult — the type
/// minimal-API endpoints (MapGet/MapPost/...) actually execute correctly.
/// Use this in new minimal-API slices. ResultExtensions.ToActionResult() returns
/// the older Mvc.IActionResult, kept only for the legacy Controllers.
/// </summary>
public static class MinimalResultExtensions
{
    public static IResult ToMinimalResult<T>(this Result<T> result)
        => result switch
        {
            Result<T>.SuccessResult success => Results.Ok(success.DataValue),
            Result<T>.FailureResult failure => CreateProblem(failure.ErrorValue!),
            _ => Results.StatusCode(500)
        };

    public static IResult ToMinimalResult(this Result result)
        => result switch
        {
            Result.SuccessResult => Results.NoContent(),
            Result.FailureResult failure => CreateProblem(failure.ErrorValue!),
            _ => Results.StatusCode(500)
        };

    // Exposed for endpoints that need the correct status-code-per-ErrorType mapping (404 for
    // NotFound, 409 for Conflict, etc.) but keep a custom success response shape, so they can't
    // just call ToMinimalResult() wholesale — e.g. DeleteTask/DeleteFolder/DeleteSpace return
    // `{ SyncEventId }` on success. Without this, endpoints calling Results.BadRequest(result.Error)
    // unconditionally return 400 for everything, including NotFound — which the frontend's
    // mutation rollback logic can't distinguish from a real failure that needs undoing.
    public static IResult Problem(Error error) => CreateProblem(error);

    private static IResult CreateProblem(Error error)
    {
        var statusCode = MapErrorTypeToStatusCode(error.Type);
        return Results.Problem(
            type: $"https://httpstatuses.com/{statusCode}",
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode
        );
    }

    private static int MapErrorTypeToStatusCode(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status400BadRequest
    };
}
