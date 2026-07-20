using TacticalGoap.Abstractions.Enums;

namespace TacticalGoap.Runtime.WorldState;

/// <summary>
/// Set or clear effect applied to a single symbolic world fact.
/// </summary>
public readonly struct WorldStateEffect
{
    /// <summary>
    /// Initializes a set-effect that writes <paramref name="value"/> onto <paramref name="factId"/>.
    /// </summary>
    /// <param name="factId">Fact identifier.</param>
    /// <param name="value">Value written by the effect.</param>
    /// <returns>A set effect.</returns>
    public static WorldStateEffect Set(WorldFactId factId, int value)
    {
        return new WorldStateEffect(factId, value, clears: false);
    }

    /// <summary>
    /// Initializes a clear-effect that removes <paramref name="factId"/> from the state.
    /// </summary>
    /// <param name="factId">Fact identifier.</param>
    /// <returns>A clear effect.</returns>
    public static WorldStateEffect Clear(WorldFactId factId)
    {
        return new WorldStateEffect(factId, value: 0, clears: true);
    }

    private WorldStateEffect(WorldFactId factId, int value, bool clears)
    {
        FactId = factId;
        Value = value;
        Clears = clears;
    }

    /// <summary>
    /// Gets the fact identifier.
    /// </summary>
    public WorldFactId FactId { get; }

    /// <summary>
    /// Gets the value written when <see cref="Clears"/> is <see langword="false"/>.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Gets a value indicating whether this effect clears the fact.
    /// </summary>
    public bool Clears { get; }
}
