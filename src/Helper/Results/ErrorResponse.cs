using System;
using System.Collections.Generic;

namespace src.Helper.Results;

public class ErrorResponse
{
    public string Type { get; set; } = "about:blank";
    public string Title { get; set; } = "";
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public Dictionary<string, string> Extensions { get; set; } = new();

    public bool HasErrors => Extensions.Count > 0;

    // --- General Errors ---

    public static ErrorResponse Internal(string? detail = "An unexpected internal error occurred.")
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/internal-server-error",
            Title = "Internal Server Error",
            Status = 500,
            Detail = detail
        };
    }

    // --- 400 Bad Request Errors ---

    public static ErrorResponse BadRequest(string title, string? detail = null)
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/bad-request",
            Title = title,
            Status = 400,
            Detail = detail
        };
    }

   
    // --- 401 Unauthorized ---

    public static ErrorResponse Unauthorized(string? detail = "Authentication credentials are required to access this resource.")
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/unauthorized",
            Title = "Unauthorized",
            Status = 401,
            Detail = detail
        };
    }

    // --- 403 Forbidden ---

    public static ErrorResponse Forbidden(string? detail = "You do not have permission to perform this action.")
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/forbidden",
            Title = "Forbidden",
            Status = 403,
            Detail = detail
        };
    }

    // --- 404 Not Found ---

    public static ErrorResponse NotFound(string? title = "Resource Not Found", string? detail = "The requested resource could not be found.")
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/not-found",
            Title = title!,
            Status = 404,
            Detail = detail
        };
    }

    public static ErrorResponse NotFound(string resourceName, object resourceId)
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/not-found",
            Title = "Resource Not Found",
            Status = 404,
            Detail = $"The resource '{resourceName}' with ID '{resourceId}' was not found."
        };
    }

    // --- 409 Conflict ---

    public static ErrorResponse Conflict(string title, string? detail = null)
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/conflict",
            Title = title,
            Status = 409,
            Detail = detail
        };
    }

    // --- 422 Unprocessable Entity ---

    public static ErrorResponse BusinessRuleViolation(string title, string? detail = null)
    {
        return new ErrorResponse
        {
            Type = "https://example.com/errors/business-rule-violation",
            Title = title,
            Status = 422, // Unprocessable Entity is a good fit for domain errors
            Detail = detail,
        };
    }
}