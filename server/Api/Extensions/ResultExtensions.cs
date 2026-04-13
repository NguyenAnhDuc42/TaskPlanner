using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Results;
using Application.Common.Errors;

namespace Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
        => result switch
        {
            Result<T>.SuccessResult success => 
                new OkObjectResult(success.Value),

            Result<T>.FailureResult failure => 
                CreateProblemDetails(failure.Error!),

            _ => new StatusCodeResult(500)
        };

    public static IActionResult ToActionResult(this Result result)
        => result switch
        {
            Result.SuccessResult => new NoContentResult(),

            Result.FailureResult failure =>
                CreateProblemDetails(failure.Error!),

            _ => new StatusCodeResult(500)
        };

    private static ObjectResult CreateProblemDetails(Error error)
    {
        if (error == null) throw new ArgumentNullException(nameof(error));

        var statusCode = MapErrorTypeToStatusCode(error.Type);
        
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = error.Code,
            Detail = error.Description,
            Status = statusCode
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
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
