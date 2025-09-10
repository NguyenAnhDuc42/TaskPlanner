using System;
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
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        ProblemDetails problem = new ProblemDetails();
        switch (ex)
        {
            case ValidationException validationEx:
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
            case UnauthorizedAccessException _:
                problem = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to perform this action.",
                    Status = StatusCodes.Status401Unauthorized

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
