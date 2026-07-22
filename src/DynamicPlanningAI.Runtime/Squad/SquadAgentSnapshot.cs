using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Agent facts visible to the squad coordinator for one tick.
/// </summary>
/// <remarks>
/// The coordinator is not omniscient: only fields supplied in this snapshot may
/// influence clustering, behavior selection, and order monitoring.
/// </remarks>
public readonly struct SquadAgentSnapshot
{
    /// <summary>
    /// Initializes an agent snapshot.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="teamId">Team affiliation.</param>
    /// <param name="compatibilityKey">Formation compatibility key.</param>
    /// <param name="position">Known agent cell.</param>
    /// <param name="isAlive">Whether the agent is active.</param>
    /// <param name="isInDanger">Whether the agent reports immediate danger.</param>
    /// <param name="hasActiveThreat">Whether the agent knows of a combat threat.</param>
    /// <param name="needsCover">Whether the agent requests cover.</param>
    /// <param name="isSeparated">Whether the agent is separated from allies.</param>
    /// <param name="canSuppress">Whether the agent can provide suppression.</param>
    public SquadAgentSnapshot(
        AgentId agentId,
        int teamId,
        int compatibilityKey,
        Int2 position,
        bool isAlive,
        bool isInDanger,
        bool hasActiveThreat,
        bool needsCover,
        bool isSeparated,
        bool canSuppress)
    {
        AgentId = agentId;
        TeamId = teamId;
        CompatibilityKey = compatibilityKey;
        Position = position;
        IsAlive = isAlive;
        IsInDanger = isInDanger;
        HasActiveThreat = hasActiveThreat;
        NeedsCover = needsCover;
        IsSeparated = isSeparated;
        CanSuppress = canSuppress;
    }

    /// <summary>Gets the agent identifier.</summary>
    public AgentId AgentId { get; }

    /// <summary>Gets the team affiliation.</summary>
    public int TeamId { get; }

    /// <summary>Gets the formation compatibility key.</summary>
    public int CompatibilityKey { get; }

    /// <summary>Gets the known agent cell.</summary>
    public Int2 Position { get; }

    /// <summary>Gets a value indicating whether the agent is active.</summary>
    public bool IsAlive { get; }

    /// <summary>Gets a value indicating immediate danger.</summary>
    public bool IsInDanger { get; }

    /// <summary>Gets a value indicating a known combat threat.</summary>
    public bool HasActiveThreat { get; }

    /// <summary>Gets a value indicating a cover request.</summary>
    public bool NeedsCover { get; }

    /// <summary>Gets a value indicating separation from allies.</summary>
    public bool IsSeparated { get; }

    /// <summary>Gets a value indicating suppression capability.</summary>
    public bool CanSuppress { get; }
}
