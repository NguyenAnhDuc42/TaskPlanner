using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Api;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _env = env ?? throw new ArgumentNullException(nameof(env));
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
            await HandleExceptionAsync(context, ex, traceId, _env.IsDevelopment());
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex, string traceId, bool isDevelopment)
    {
        ProblemDetails problem;
        string code;
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
                code = "Validation.Failed";
                break;
            case UnauthorizedAccessException unAuth:
                problem = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = $"{unAuth.Message}",
                    Status = StatusCodes.Status401Unauthorized

                };
                code = "Auth.Unauthorized";
                break;
            case InvalidTokenException invalidTokenEx:
                problem = new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = invalidTokenEx.Message,
                    Status = StatusCodes.Status400BadRequest
                };
                code = "Request.InvalidToken";
                break;
            case BusinessRuleException businessEx:
                problem = new ProblemDetails
                {
                    Title = "Business Rule Violation",
                    Detail = businessEx.Message,
                    Status = StatusCodes.Status400BadRequest
                };
                code = "BusinessRule.Violation";
                break;
            case KeyNotFoundException keyNotFoundEx:
                problem = new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = keyNotFoundEx.Message,
                    Status = StatusCodes.Status404NotFound
                };
                code = "Entity.NotFound";
                break;
            case DbUpdateConcurrencyException:
                problem = new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = "The resource was modified by another user. Please refresh and try again.",
                    Status = StatusCodes.Status409Conflict
                };
                code = "Concurrency.Conflict";
                break;
            case OperationCanceledException:
                problem = new ProblemDetails
                {
                    Title = "Request Canceled",
                    Detail = "The request was canceled by the client.",
                    Status = 499 // Client Closed Request
                };
                code = "Request.Canceled";
                break;
            default:
                problem = new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = isDevelopment
                        ? $"{ex.Message}\n\n{ex.StackTrace}"
                        : "Something went wrong on our end. If this keeps happening, contact support with the trace ID below.",
                    Status = StatusCodes.Status500InternalServerError,
                    Extensions = { ["traceId"] = traceId }
                };
                code = "Internal.ServerError";
                break;
        }

        problem.Type = $"https://httpstatuses.com/{problem.Status}";
        problem.Extensions[ErrorResponseShape.CodeExtensionKey] = code;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return context.Response.WriteAsJsonAsync(problem);

    }

}


