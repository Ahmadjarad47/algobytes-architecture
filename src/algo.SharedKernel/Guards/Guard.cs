using System.Runtime.CompilerServices;

namespace algo.SharedKernel.Guards;

/// <summary>
/// Lightweight guard clauses to enforce preconditions at aggregate / value-object boundaries.
/// All methods return the validated value so guards can be composed inline.
/// </summary>
public static class Guard
{
    /// <summary>Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.</summary>
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is null or whitespace.</summary>
    public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is not positive.</summary>
    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.");
        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is negative.</summary>
    public static int NotNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must not be negative.");
        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> falls outside the specified range.</summary>
    public static T Range<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
        return value;
    }
}
