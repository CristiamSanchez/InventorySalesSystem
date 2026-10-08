namespace SistemaInventarioVentas.Application.Common;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, ApplicationError? error)
    {
        _value = value;
        Error = error;
        IsSuccess = error is null;
    }

    public bool IsSuccess { get; }

    public ApplicationError? Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result does not contain a value.");

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(value, null);
    }

    public static Result<T> Failure(ApplicationErrorCode code, string message) =>
        new(default, new ApplicationError(code, message));
}
