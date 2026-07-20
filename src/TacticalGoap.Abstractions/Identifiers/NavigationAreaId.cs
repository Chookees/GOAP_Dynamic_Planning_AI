using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace TacticalGoap.Abstractions.Identifiers;

/// <summary>
/// Stable navigation area identifier used in fixed-capacity runtime tables.
/// </summary>
/// <remarks>
/// The identifier is assigned during initialization or record creation and is
/// never reused while the owning table entry remains live. The sentinel
/// <see cref="Invalid"/> represents the absence of a navigation area. Conversion from
/// arbitrary integers is explicit and validated.
/// </remarks>
public readonly struct NavigationAreaId : IEquatable<NavigationAreaId>, IComparable<NavigationAreaId>
{
    /// <summary>
    /// Sentinel value representing no navigation area.
    /// </summary>
    public static readonly NavigationAreaId Invalid = new(-1);

    private readonly int _value;

    private NavigationAreaId(int value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the raw non-negative table index.
    /// </summary>
    public int Value => _value;

    /// <summary>
    /// Gets a value indicating whether this identifier is the invalid sentinel.
    /// </summary>
    public bool IsValid => _value >= 0;

    /// <summary>
    /// Creates a navigation area identifier from a non-negative raw index.
    /// </summary>
    /// <param name="value">Zero-based table index.</param>
    /// <returns>A validated identifier.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is negative.
    /// </exception>
    public static NavigationAreaId FromInt32(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "NavigationAreaId must be non-negative.");
        }

        return new NavigationAreaId(value);
    }

    /// <summary>
    /// Attempts to create a navigation area identifier from a raw index.
    /// </summary>
    /// <param name="value">Candidate raw index.</param>
    /// <param name="id">Receives the identifier when successful.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is non-negative.</returns>
    public static bool TryFromInt32(int value, out NavigationAreaId id)
    {
        if (value < 0)
        {
            id = Invalid;
            return false;
        }

        id = new NavigationAreaId(value);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(NavigationAreaId other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is NavigationAreaId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value;

    /// <inheritdoc />
    public int CompareTo(NavigationAreaId other) => _value.CompareTo(other._value);

    /// <inheritdoc />
    public override string ToString() =>
        IsValid ? _value.ToString(CultureInfo.InvariantCulture) : "Invalid";

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(NavigationAreaId left, NavigationAreaId right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(NavigationAreaId left, NavigationAreaId right) => !left.Equals(right);
}
