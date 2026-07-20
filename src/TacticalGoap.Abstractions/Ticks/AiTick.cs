using System;

namespace TacticalGoap.Abstractions.Ticks;

/// <summary>
/// Host-provided deterministic tick stamp driving the AI runtime.
/// </summary>
/// <remarks>
/// The runtime never reads wall-clock time. The host must serialize calls and
/// ensure <see cref="Sequence"/> never decreases. <see cref="DeltaMilliseconds"/>
/// must be positive and within the configured hard maximum.
/// </remarks>
public readonly struct AiTick : IEquatable<AiTick>
{
    /// <summary>
    /// Initializes a new tick stamp.
    /// </summary>
    /// <param name="sequence">Monotonic tick sequence number.</param>
    /// <param name="deltaMilliseconds">Elapsed simulated milliseconds since the previous tick.</param>
    public AiTick(long sequence, int deltaMilliseconds)
    {
        Sequence = sequence;
        DeltaMilliseconds = deltaMilliseconds;
    }

    /// <summary>
    /// Gets the monotonic tick sequence.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Gets the simulated elapsed milliseconds for this tick.
    /// </summary>
    public int DeltaMilliseconds { get; }

    /// <inheritdoc />
    public bool Equals(AiTick other) =>
        Sequence == other.Sequence && DeltaMilliseconds == other.DeltaMilliseconds;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AiTick other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Sequence, DeltaMilliseconds);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(AiTick left, AiTick right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(AiTick left, AiTick right) => !left.Equals(right);
}
