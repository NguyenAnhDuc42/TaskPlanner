using System;

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

    public static ErrorResponse BussinessError(string title, string? detail = null)
    {
        return new ErrorResponse
        {
            Type = "https://example.com/business-error",
            Title = title,
            Status = 400,
            Detail = detail,
        };
    }

    public static ErrorResponse NotFound(string? title = "Resource not found", string? detail = "The requested resource could not be found.")
    {
        return new ErrorResponse
        {
            Type = "https://example.com/not-found",
            Title = title!,
            Status = 404,
            Detail = detail
        };
    }
    public static ErrorResponse BadRequest(string title, string detail)
    {
        return new ErrorResponse
        {
            Type = "https://yourdomain.com/errors/bad-request",
            Title = title,
            Status = 400,
            Detail = detail
        };
    }
    public static ErrorResponse Conflict(string title, string detail)
    {
        return new ErrorResponse
        {
            Type = "https://yourdomain.com/errors/conflict",
            Title = title,
            Status = 409,
            Detail = detail
        };
    }

    public static ErrorResponse Unauthorized(string title, string detail)
    {
        return new ErrorResponse
        {
            Type = "https://yourdomain.com/errors/unauthorized",
            Title = title,
            Status = 401,
            Detail = detail
        };
    }
    public static ErrorResponse Internal(string message)
    {
        return new ErrorResponse
        {
            Type = "https://yourdomain.com/errors/internal-error",
            Title = "Internal Server Error",
            Status = 500,
            Detail = message
        };

    }
}
