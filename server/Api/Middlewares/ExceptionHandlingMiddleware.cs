using Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var exceptionType = ex.GetType().FullName;
        var isFluentValidation = ex is ValidationException;
        Console.WriteLine($"Exception Type: {exceptionType}");
        Console.WriteLine($"Is FluentValidation.ValidationException: {isFluentValidation}");

        ProblemDetails problem = new ProblemDetails();
        switch (ex)
        {
            case ValidationException validationEx:
                Console.WriteLine("MATCHED ValidationException case");
                problem = new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Extensions =
                    {
                        ["errors"] = validationEx.Errors
                            .Select(e => new { e.PropertyName, e.ErrorMessage })
                    }
                };
                break;
            case UnauthorizedAccessException unAuth:
                problem = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = $"{unAuth.Message}",
                    Status = StatusCodes.Status401Unauthorized

                };
                break;
            case ForbiddenAccessException forbiddenEx:
                problem = new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = forbiddenEx.Message,
                    Status = StatusCodes.Status403Forbidden
                };
                break;
            case DuplicateEmailException duplicateEx:
                problem = new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = duplicateEx.Message,
                    Status = StatusCodes.Status409Conflict
                };
                break;
            case InvalidTokenException invalidTokenEx:
                problem = new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = invalidTokenEx.Message,
                    Status = StatusCodes.Status400BadRequest
                };
                break;
            default:
                problem = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Status = StatusCodes.Status500InternalServerError
                };
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return context.Response.WriteAsJsonAsync(problem);

    }

}
