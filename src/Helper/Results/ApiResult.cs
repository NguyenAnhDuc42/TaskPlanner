using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace src.Helper.Results;

public class ApiResult<T> : IActionResult
{
    public T? Value { get; }
    public ErrorResponse? Error { get; }
    public bool IsSuccess => Error == null;
    private ApiResult(T value) => Value = value;
    private ApiResult(ErrorResponse error) => Error = error;
    public static ApiResult<T> Success(T value) => new(value);
    public static ApiResult<T> Failure(ErrorResponse error) => new(error);
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var rep = context.HttpContext.Response;
        rep.ContentType = "application/json";
        if (IsSuccess)
        {
            rep.StatusCode = 200;
            await rep.WriteAsJsonAsync(Value, JsonOptions);
        }
        else
        {
            rep.StatusCode = Error!.Status;
            await rep.WriteAsJsonAsync(Error, JsonOptions);
        }
    }
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

public static class ApiResultExtensions
{
    public static ApiResult<T> ToApiResult<T,TError>(this Result<T, TError> result) where TError : ErrorResponse
    {
        return result.IsSuccess
            ? ApiResult<T>.Success(result.Value)
            : ApiResult<T>.Failure(result.Error);
    }
}
