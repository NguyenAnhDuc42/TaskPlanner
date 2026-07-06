using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace Api;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
        => result switch
        {
            Result<T>.SuccessResult success => 
                new OkObjectResult(success.DataValue),

            Result<T>.FailureResult failure => 
                CreateProblemDetails(failure.ErrorValue!),

            _ => new StatusCodeResult(500)
        };

    public static IActionResult ToActionResult(this Result result)
        => result switch
        {
            Result.SuccessResult => new NoContentResult(),

            Result.FailureResult failure =>
                CreateProblemDetails(failure.ErrorValue!),

            _ => new StatusCodeResult(500)
        };

    private static ObjectResult CreateProblemDetails(Error error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));

        var statusCode = ErrorResponseShape.MapErrorTypeToStatusCode(error.Type);

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = ErrorResponseShape.TitleForStatusCode(statusCode),
            Detail = error.Description,
            Status = statusCode
        };
        problemDetails.Extensions[ErrorResponseShape.CodeExtensionKey] = error.Code;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }
}


