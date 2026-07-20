using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace TacticalGoap.Abstractions.Identifiers;

/// <summary>
/// Stable agent table index used to address agents without retaining object references.
/// </summary>
/// <remarks>
/// Identifiers are assigned during initialization and remain unchanged until the
/// runtime is disposed. A raw value outside the configured agent capacity is
/// invalid and must be rejected at subsystem boundaries. The sentinel
/// <see cref="Invalid"/> represents the absence of an agent.
/// </remarks>
public readonly struct AgentId : IEquatable<AgentId>, IComparable<AgentId>
{
    /// <summary>
    /// Sentinel value representing no agent.
    /// </summary>
    public static readonly AgentId Invalid = new(-1);

    private readonly int _value;

    private AgentId(int value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the raw zero-based table index.
    /// </summary>
    public int Value => _value;

    /// <summary>
    /// Gets a value indicating whether this identifier is the invalid sentinel.
    /// </summary>
    public bool IsValid => _value >= 0;

    /// <summary>
    /// Creates an agent identifier from a non-negative raw index.
    /// </summary>
    /// <param name="value">Zero-based agent table index.</param>
    /// <returns>A validated agent identifier.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is negative.
    /// </exception>
    public static AgentId FromInt32(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "AgentId must be non-negative.");
        }

        return new AgentId(value);
    }

    /// <summary>
    /// Attempts to create an agent identifier from a raw index.
    /// </summary>
    /// <param name="value">Candidate raw index.</param>
    /// <param name="id">Receives the identifier when successful.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is non-negative.</returns>
    public static bool TryFromInt32(int value, out AgentId id)
    {
        if (value < 0)
        {
            id = Invalid;
            return false;
        }

        id = new AgentId(value);
        return true;
    }

    /// <inheritdoc />
    public bool Equals(AgentId other) => _value == other._value;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is AgentId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _value;

    /// <inheritdoc />
    public int CompareTo(AgentId other) => _value.CompareTo(other._value);

    /// <inheritdoc />
    public override string ToString() =>
        IsValid ? _value.ToString(CultureInfo.InvariantCulture) : "Invalid";

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(AgentId left, AgentId right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(AgentId left, AgentId right) => !left.Equals(right);
}
