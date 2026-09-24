namespace algo.SharedKernel.Primitives;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");
    public static readonly Error NotFound = new("Error.NotFound", "The requested resource was not found.");
    public static readonly Error Unauthorized = new("Error.Unauthorized", "Access is not authorized.");
    public static readonly Error Forbidden = new("Error.Forbidden", "Access to this resource is forbidden.");

    public static Error Validation(string field, string message)
        => new($"Validation.{field}", message);

    public static Error Conflict(string code, string message)
        => new($"Conflict.{code}", message);
}
