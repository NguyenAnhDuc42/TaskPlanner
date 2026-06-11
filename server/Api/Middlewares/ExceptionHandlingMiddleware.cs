using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Domain;
using Microsoft.EntityFrameworkCore;
using Application;
using System.Diagnostics;

namespace Api;

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
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception on {RequestPath} [TraceId: {TraceId}]: {Message}", context.Request.Path, traceId, ex.Message);
            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex, string traceId)
    {
        ProblemDetails problem;
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
            case UnauthorizedAccessException unAuth:
                problem = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = $"{unAuth.Message}",
                    Status = StatusCodes.Status401Unauthorized

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
            case BusinessRuleException businessEx:
                problem = new ProblemDetails
                {
                    Title = "Business Rule Violation",
                    Detail = businessEx.Message,
                    Status = StatusCodes.Status400BadRequest
                };
                break;
            case KeyNotFoundException keyNotFoundEx:
                problem = new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = keyNotFoundEx.Message,
                    Status = StatusCodes.Status404NotFound
                };
                break;
            case DbUpdateConcurrencyException:
                problem = new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = "The resource was modified by another user. Please refresh and try again.",
                    Status = StatusCodes.Status409Conflict
                };
                break;
            case OperationCanceledException:
                problem = new ProblemDetails
                {
                    Title = "Request Canceled",
                    Detail = "The request was canceled by the client.",
                    Status = 499 // Client Closed Request
                };
                break;
            default:
                problem = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Status = StatusCodes.Status500InternalServerError,
                    Extensions = { ["traceId"] = traceId }
                };
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return context.Response.WriteAsJsonAsync(problem);

    }

}


