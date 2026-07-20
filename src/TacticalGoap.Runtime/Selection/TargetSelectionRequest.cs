using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Runtime.Selection;

/// <summary>
/// Inputs for deterministic target focus selection.
/// </summary>
public struct TargetSelectionRequest
{
    /// <summary>
    /// Gets or sets the selecting agent.
    /// </summary>
    public AgentId Agent { get; set; }

    /// <summary>
    /// Gets or sets the agent squad, when any.
    /// </summary>
    public SquadId Squad { get; set; }

    /// <summary>
    /// Gets or sets the agent cell position.
    /// </summary>
    public Int2 AgentPosition { get; set; }

    /// <summary>
    /// Gets or sets the current tick sequence.
    /// </summary>
    public long CurrentTick { get; set; }

    /// <summary>
    /// Gets or sets the current focus target for hysteresis, or invalid.
    /// </summary>
    public EntityId CurrentFocus { get; set; }

    /// <summary>
    /// Gets or sets the sticky focus bonus applied to the current focus.
    /// </summary>
    public int HysteresisBonus { get; set; }

    /// <summary>
    /// Gets or sets the maximum memory candidates considered.
    /// </summary>
    public int MaxCandidates { get; set; }
}

/// <summary>
/// Result of target focus selection.
/// </summary>
public readonly struct TargetSelectionResult
{
    /// <summary>
    /// Initializes a selection result.
    /// </summary>
    /// <param name="selected">Selected focus entity.</param>
    /// <param name="score">Winning score.</param>
    /// <param name="changed">Whether the focus changed.</param>
    public TargetSelectionResult(EntityId selected, int score, bool changed)
    {
        Selected = selected;
        Score = score;
        Changed = changed;
    }

    /// <summary>
    /// Gets the selected focus entity.
    /// </summary>
    public EntityId Selected { get; }

    /// <summary>
    /// Gets the winning score.
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// Gets whether the focus changed from the previous value.
    /// </summary>
    public bool Changed { get; }
}
