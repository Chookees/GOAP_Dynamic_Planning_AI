using DynamicPlanningAI.Abstractions.Enums;

namespace DynamicPlanningAI.Runtime.WorldState;

/// <summary>
/// Required equality condition against a single symbolic world fact.
/// </summary>
/// <remarks>
/// When <see cref="RequiredSpecified"/> is <see langword="false"/>, the fact
/// must be unspecified in the evaluated state. Otherwise the fact must be
/// present with <see cref="RequiredValue"/>.
/// </remarks>
public readonly struct WorldStateCondition
{
    /// <summary>
    /// Initializes a new condition.
    /// </summary>
    /// <param name="factId">Fact identifier.</param>
    /// <param name="requiredValue">Required value when the fact must be specified.</param>
    /// <param name="requiredSpecified">Whether the fact must be specified.</param>
    public WorldStateCondition(WorldFactId factId, int requiredValue, bool requiredSpecified)
    {
        FactId = factId;
        RequiredValue = requiredValue;
        RequiredSpecified = requiredSpecified;
    }

    /// <summary>
    /// Gets the fact identifier.
    /// </summary>
    public WorldFactId FactId { get; }

    /// <summary>
    /// Gets the required value when <see cref="RequiredSpecified"/> is true.
    /// </summary>
    public int RequiredValue { get; }

    /// <summary>
    /// Gets a value indicating whether the fact must be specified.
    /// </summary>
    public bool RequiredSpecified { get; }
}
