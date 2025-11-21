namespace FluentSourceGenerators;

/// <summary>
/// Helper class required for init-only properties when targeting netstandard2.0.
/// This exact name and namespace is recognized by the C# compiler as the built-in
/// System.Runtime.CompilerServices.IsExternalInit when the real one is missing.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class IsExternalInit
{
    // Empty – the type existence is enough for the compiler.
}

/// <summary>
/// Represents the result of a transformation in a source generator pipeline.
/// Enables the "pit of success" by allowing early diagnostic reporting without null checks.
/// </summary>
/// <typeparam name="T">The model type produced on success.</typeparam>
public readonly struct GenResult<T>
{
    /// <summary>
    /// The successful value, or default if the operation failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The diagnostic that describes the failure, or null if the operation succeeded.
    /// </summary>
    public Diagnostic? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    private GenResult(T value, Diagnostic? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GenResult<T> Success(T value) => new(value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GenResult<T> Fail(Diagnostic error) => new(default, error);

    /// <summary>
    /// Gets a value indicating whether the transformation succeeded.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Implicit conversion from T → GenResult&lt;T&gt; (success)
    /// </summary>
    public static implicit operator GenResult<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from Diagnostic → GenResult&lt;T&gt; (failure)
    /// </summary>
    public static implicit operator GenResult<T>(Diagnostic error) => Fail(error);
}
