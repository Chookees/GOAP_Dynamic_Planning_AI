using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Runtime.Squad;

/// <summary>
/// Cover candidate visible to the squad coordinator for one tick.
/// </summary>
public readonly struct SquadCoverCandidate
{
    /// <summary>
    /// Initializes a cover candidate.
    /// </summary>
    /// <param name="pointId">Tactical point identifier.</param>
    /// <param name="position">Point cell.</param>
    /// <param name="capacity">Remaining reservation capacity units.</param>
    public SquadCoverCandidate(TacticalPointId pointId, Int2 position, int capacity)
    {
        PointId = pointId;
        Position = position;
        Capacity = capacity;
    }

    /// <summary>Gets the tactical point identifier.</summary>
    public TacticalPointId PointId { get; }

    /// <summary>Gets the point cell.</summary>
    public Int2 Position { get; }

    /// <summary>Gets remaining reservation capacity.</summary>
    public int Capacity { get; }
}
