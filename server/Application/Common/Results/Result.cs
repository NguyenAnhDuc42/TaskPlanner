using Application.Common.Errors;

namespace Application.Common.Results;

public abstract record Result
{
    public sealed record Success : Result;
    public sealed record Failure(Error Error) : Result;

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public static Result Success() => new Success();
    public static Result Failure(Error error) => new Failure(error);

    public static implicit operator Result(Error error) => new Failure(error);
}

public abstract record Result<TValue>
{
    public sealed record Success(TValue Value) : Result<TValue>;
    public sealed record Failure(Error Error) : Result<TValue>;

    public bool IsSuccess => this is Result<TValue>.Success;
    public bool IsFailure => this is Result<TValue>.Failure;

    public static Result<TValue> Success(TValue value) => new Success(value);
    public static Result<TValue> Failure(Error error) => new Failure(error);

    public static implicit operator Result<TValue>(TValue value) => new Success(value);
    public static implicit operator Result<TValue>(Error error) => new Failure(error);
}
