using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace TacticalGoap.Abstractions.Identifiers;

/// <summary>
/// Stable navigation node identifier used in fixed-capacity runtime tables.
/// </summary>
/// <remarks>
/// The identifier is assigned during initialization or record creation and is
/// never reused while the owning table entry remains live. The sentinel
/// <see cref="Invalid"/> represents the absence of a navigation node. Conversion from
/// arbitrary integers is explicit and validated.
/// </remarks>
public readonly struct NavigationNodeId : IEquatable<NavigationNodeId>, IComparable<NavigationNodeId>
{
    /// <summary>
    /// Sentinel value representing no navigation node.
    /// </summary>
    public static readonly NavigationNodeId Invalid = new(-1);

    private readonly int _value;

    private NavigationNodeId(int value)
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
    /// Creates a navigation node identifier from a non-negative raw index.
    /// </summary>
    /// <param name="value">Zero-based table index.</param>
    /// <returns>A validated identifier.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is negative.
    /// </exception>
    public static NavigationNodeId FromInt32(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "NavigationNodeId must be non-negative.");
        }

        return new NavigationNodeId(value);
    }

    /// <summary>
    /// Attempts to create a navigation node identifier from a raw index.
    /// </summary>
    /// <param name="value">Candidate raw index.</param>
    /// <param name="id">Receives the identifier when successful.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is non-negative.</returns>
    public static bool TryFromInt32(int value, out NavigationNodeId id)
    {
        if (value < 0)
        {
            id = Invalid;
            return false;
        }

        id = new NavigationNodeId(value);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(NavigationNodeId other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is NavigationNodeId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value;

    /// <inheritdoc />
    public int CompareTo(NavigationNodeId other) => _value.CompareTo(other._value);

    /// <inheritdoc />
    public override string ToString() =>
        IsValid ? _value.ToString(CultureInfo.InvariantCulture) : "Invalid";

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(NavigationNodeId left, NavigationNodeId right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(NavigationNodeId left, NavigationNodeId right) => !left.Equals(right);
}
