namespace Api;

public abstract record Result
{
    public sealed record SuccessResult : Result
    {
        public override bool IsSuccess => true;
        public override bool IsFailure => false;
        public override Error? Error => null;
    }
    
    public sealed record FailureResult(Error ErrorValue) : Result
    {
        public override bool IsSuccess => false;
        public override bool IsFailure => true;
        public override Error? Error => ErrorValue;
    }

    public abstract bool IsSuccess { get; }
    public abstract bool IsFailure { get; }
    public abstract Error? Error { get; }

    public static Result Success() => new SuccessResult();
    public static Result Failure(Error error) => new FailureResult(error);

    public static implicit operator Result(Error error) => new FailureResult(error);
}

public abstract record Result<TValue>
{
    public sealed record SuccessResult(TValue DataValue) : Result<TValue>
    {
        public override bool IsSuccess => true;
        public override bool IsFailure => false;
        public override Error? Error => null;
        public override TValue? Value => DataValue;
    }
    
    public sealed record FailureResult(Error ErrorValue) : Result<TValue>
    {
        public override bool IsSuccess => false;
        public override bool IsFailure => true;
        public override Error? Error => ErrorValue;
        public override TValue? Value => default;
    }

    public abstract bool IsSuccess { get; }
    public abstract bool IsFailure { get; }
    public abstract Error? Error { get; }
    public abstract TValue? Value { get; }

    public static Result<TValue> Success(TValue value) => new SuccessResult(value);
    public static Result<TValue> Failure(Error error) => new FailureResult(error);

    public static implicit operator Result<TValue>(TValue value) => new SuccessResult(value);
    public static implicit operator Result<TValue>(Error error) => new FailureResult(error);
}
