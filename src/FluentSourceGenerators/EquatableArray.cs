namespace FluentSourceGenerators;

/// <summary>
/// A wrapper around ImmutableArray&lt;T&gt; that provides value-based equality and hashing.
/// Required for correct incremental generator caching when collections are part of the model.
/// </summary>
/// <typeparam name="T">Element type that supports equality.</typeparam>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    /// <summary>
    /// Gets the underlying immutable array.
    /// </summary>
    public ImmutableArray<T> Array => _array;

    /// <summary>
    /// Creates a new instance from an ImmutableArray.
    /// </summary>
    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array;
    }

    /// <summary>
    /// Returns true if both arrays contain the same elements in the same order.
    /// </summary>
    public bool Equals(EquatableArray<T> other)
        => _array.SequenceEqual(other._array);

    /// <summary>
    /// Returns a hash code based on the array contents.
    /// Compatible with netstandard2.0 – no HashCode struct needed.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in _array)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);
    /// <summary>
    ///
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static implicit operator EquatableArray<T>(T[]? array)
        => array is null ? default : new(array.ToImmutableArray());
}
