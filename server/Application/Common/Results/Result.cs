using Application.Common.Errors;

namespace Application.Common.Results;

#pragma warning disable CS8907
public abstract record Result
{
    public sealed record SuccessResult : Result;
    public sealed record FailureResult(Error ErrorValue) : Result;

    public bool IsSuccess => this is SuccessResult;
    public bool IsFailure => this is FailureResult;
    public virtual Error? Error => this is FailureResult failure ? failure.ErrorValue : null;

    public static Result Success() => new SuccessResult();
    public static Result Failure(Error error) => new FailureResult(error);

    public static implicit operator Result(Error error) => new FailureResult(error);
}

public abstract record Result<TValue>
{
    public sealed record SuccessResult(TValue DataValue) : Result<TValue>;
    public sealed record FailureResult(Error ErrorValue) : Result<TValue>;

    public bool IsSuccess => this is SuccessResult;
    public bool IsFailure => this is FailureResult;
    public virtual Error? Error => this is FailureResult failure ? failure.ErrorValue : null;
    public virtual TValue? Value => this is SuccessResult success ? success.DataValue : default;

    public static Result<TValue> Success(TValue value) => new SuccessResult(value);
    public static Result<TValue> Failure(Error error) => new FailureResult(error);

    public static implicit operator Result<TValue>(TValue value) => new SuccessResult(value);
    public static implicit operator Result<TValue>(Error error) => new FailureResult(error);
}
#pragma warning restore CS8907