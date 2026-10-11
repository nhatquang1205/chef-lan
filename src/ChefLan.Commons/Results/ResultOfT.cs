namespace ChefLan.Commons.Results;

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    internal static Result<TValue> FromValue(TValue value) => new(value, true, Error.None);

    internal static Result<TValue> FromError(Error error) => new(default, false, error);

    public static implicit operator Result<TValue>(TValue value) => FromValue(value);

    public static implicit operator Result<TValue>(Error error) => FromError(error);
}
