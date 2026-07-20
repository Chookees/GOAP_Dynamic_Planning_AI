using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Host-provided read-only agent body snapshot for a single agent.
/// </summary>
/// <remarks>
/// Implementations must expose deterministic, allocation-free property reads.
/// The runtime does not retain the interface beyond the current tick callback.
/// Position and facing are discrete; hosts quantize continuous transforms before
/// returning values.
/// </remarks>
public interface IAgentBody
{
    /// <summary>
    /// Gets the agent cell position.
    /// </summary>
    public Int2 Position { get; }

    /// <summary>
    /// Gets the agent facing direction.
    /// </summary>
    public Direction8 Facing { get; }

    /// <summary>
    /// Gets the agent posture.
    /// </summary>
    public AgentStance Stance { get; }

    /// <summary>
    /// Gets a value indicating whether the agent is alive.
    /// </summary>
    public bool IsAlive { get; }

    /// <summary>
    /// Gets a value indicating whether the agent is incapacitated but not dead.
    /// </summary>
    public bool IsIncapacitated { get; }
}
