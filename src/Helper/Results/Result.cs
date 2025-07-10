

public class Result<T, TError>
{
    private T? _value;
    private TError? _error;
    public bool IsSuccess { get; }
    public T Value
    {
        get => IsSuccess ? _value! : throw new InvalidOperationException("Result is not successful.");
        private set => _value = value;
    }
    public TError Error
    { 
        get => !IsSuccess ? _error! : throw new InvalidOperationException("Result is successful.");
        private set => _error = value; 
    }

    private Result(bool isSuccess, T? value, TError? error)
        => (IsSuccess, _value, _error) = (isSuccess, value, error);
    public static Result<T, TError> Success(T value)
        => new Result<T, TError>(true, value, default);
    public static Result<T, TError> Failure(TError error)
        => new Result<T, TError>(false, default, error);

}
