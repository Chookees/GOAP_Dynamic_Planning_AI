using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Bounded squad registry entry.
/// </summary>
public struct SquadRecord
{
    /// <summary>
    /// Initializes an inactive squad record.
    /// </summary>
    /// <param name="id">Stable squad identifier.</param>
    public SquadRecord(SquadId id)
    {
        Id = id;
        TeamId = 0;
        CompatibilityKey = 0;
        IsActive = false;
        ActiveBehavior = SquadBehaviorType.None;
        BehaviorStartedTick = 0L;
        BehaviorExpiryTick = 0L;
        FocusPosition = Int2.Zero;
        MemberCount = 0;
        OrdersIssuedThisBehavior = 0;
    }

    /// <summary>Gets or sets the stable squad identifier.</summary>
    public SquadId Id { get; set; }

    /// <summary>Gets or sets the team affiliation used for clustering.</summary>
    public int TeamId { get; set; }

    /// <summary>Gets or sets the compatibility key used for clustering.</summary>
    public int CompatibilityKey { get; set; }

    /// <summary>Gets or sets a value indicating whether the squad is live.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the single active behavior; <see cref="SquadBehaviorType.None"/> when idle.</summary>
    public SquadBehaviorType ActiveBehavior { get; set; }

    /// <summary>Gets or sets the tick when the active behavior started.</summary>
    public long BehaviorStartedTick { get; set; }

    /// <summary>Gets or sets the tick when the active behavior times out.</summary>
    public long BehaviorExpiryTick { get; set; }

    /// <summary>Gets or sets the squad focus / rally / threat cell.</summary>
    public Int2 FocusPosition { get; set; }

    /// <summary>Gets or sets the number of occupied member slots.</summary>
    public int MemberCount { get; set; }

    /// <summary>Gets or sets orders issued under the current behavior.</summary>
    public int OrdersIssuedThisBehavior { get; set; }

    /// <summary>
    /// Clears behavior and membership counters while preserving the identifier.
    /// </summary>
    public void Reset()
    {
        TeamId = 0;
        CompatibilityKey = 0;
        IsActive = false;
        ActiveBehavior = SquadBehaviorType.None;
        BehaviorStartedTick = 0L;
        BehaviorExpiryTick = 0L;
        FocusPosition = Int2.Zero;
        MemberCount = 0;
        OrdersIssuedThisBehavior = 0;
    }
}
