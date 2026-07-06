using Microsoft.AspNetCore.Http;
namespace Api;

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
    public static IResult Problem(Error error) => CreateProblem(error);

    private static IResult CreateProblem(Error error)
    {
        var statusCode = ErrorResponseShape.MapErrorTypeToStatusCode(error.Type);
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = ErrorResponseShape.TitleForStatusCode(statusCode),
            Detail = error.Description,
            Status = statusCode
        };
        problem.Extensions[ErrorResponseShape.CodeExtensionKey] = error.Code;
        return Results.Problem(problem);
    }
}
