using System;

namespace TacticalGoap.Abstractions.Random;

/// <summary>
/// Deterministic xorshift32 pseudorandom generator as an immutable value type.
/// </summary>
/// <remarks>
/// State advances by returning a new <see cref="DeterministicRandom"/> instance.
/// Do not use <see cref="System.Random"/>. Callers must seed explicitly during
/// configuration so replays remain bitwise deterministic across hosts.
/// </remarks>
public readonly struct DeterministicRandom : IEquatable<DeterministicRandom>
{
    private readonly uint _state;

    /// <summary>
    /// Initializes a generator from a non-zero seed.
    /// </summary>
    /// <param name="seed">Initial state; zero is remapped to a fixed non-zero constant.</param>
    public DeterministicRandom(uint seed)
    {
        _state = seed == 0u ? 0xA5A5A5A5u : seed;
    }

    /// <summary>
    /// Gets the current generator state used as the seed for the next draw.
    /// </summary>
    public uint Seed => _state;

    /// <summary>
    /// Advances the generator and produces the next raw unsigned value.
    /// </summary>
    /// <param name="value">Receives the next xorshift32 output.</param>
    /// <returns>The advanced generator state.</returns>
    public DeterministicRandom NextUInt32(out uint value)
    {
        uint x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        value = x;
        return new DeterministicRandom(x);
    }

    /// <summary>
    /// Advances the generator and produces a non-negative integer in <c>[0, bound)</c>.
    /// </summary>
    /// <param name="bound">Exclusive upper bound; must be positive.</param>
    /// <param name="value">Receives the bounded result, or zero when <paramref name="bound"/> is not positive.</param>
    /// <returns>The advanced generator state.</returns>
    public DeterministicRandom NextInt32(int bound, out int value)
    {
        DeterministicRandom next = NextUInt32(out uint raw);
        if (bound <= 0)
        {
            value = 0;
            return next;
        }

        value = (int)(raw % (uint)bound);
        return next;
    }

    /// <summary>
    /// Advances the generator and produces a non-negative integer in <c>[0, bound)</c>.
    /// </summary>
    /// <param name="bound">Exclusive upper bound; must be positive.</param>
    /// <returns>The bounded result, or zero when <paramref name="bound"/> is not positive.</returns>
    /// <remarks>
    /// This overload discards the advanced state. Prefer
    /// <see cref="NextInt32(int, out int)"/> when the caller must retain determinism.
    /// </remarks>
    public int NextInt32(int bound)
    {
        _ = NextInt32(bound, out int value);
        return value;
    }

    /// <inheritdoc />
    public bool Equals(DeterministicRandom other) => _state == other._state;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DeterministicRandom other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((int)_state);

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><see langword="true"/> when both generators share state.</returns>
    public static bool operator ==(DeterministicRandom left, DeterministicRandom right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><see langword="true"/> when generator states differ.</returns>
    public static bool operator !=(DeterministicRandom left, DeterministicRandom right) => !left.Equals(right);
}
