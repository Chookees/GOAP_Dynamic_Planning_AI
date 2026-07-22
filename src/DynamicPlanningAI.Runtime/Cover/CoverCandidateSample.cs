using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;

namespace DynamicPlanningAI.Runtime.Cover;

/// <summary>
/// Per-candidate dynamic inputs consumed by <see cref="CoverEvaluator"/>.
/// </summary>
public readonly struct CoverCandidateSample
{
    /// <summary>
    /// Initializes a cover candidate sample.
    /// </summary>
    /// <param name="point">Static tactical point record.</param>
    /// <param name="pathCost">Integer path cost from the agent to the point.</param>
    /// <param name="occupantCount">Current occupants excluding the evaluating agent.</param>
    /// <param name="isReservedByOther">Whether another agent holds the reservation.</param>
    /// <param name="reservationHeld">Whether the evaluating agent holds the reservation.</param>
    /// <param name="hasLineOfFire">Whether LOF to the threat focus exists.</param>
    /// <param name="inGrenadeDanger">Whether the point is inside grenade danger.</param>
    /// <param name="isDestroyed">Whether the point is destroyed.</param>
    /// <param name="isNavigationAvailable">Whether the linked nav node is walkable.</param>
    /// <param name="allySeparation">Minimum Manhattan distance to nearby allies.</param>
    /// <param name="orderCompatible">Whether the point matches the active squad order.</param>
    /// <param name="invalidationReason">Precomputed invalidation reason, if any.</param>
    public CoverCandidateSample(
        in TacticalPointRecord point,
        int pathCost,
        int occupantCount,
        bool isReservedByOther,
        bool reservationHeld,
        bool hasLineOfFire,
        bool inGrenadeDanger,
        bool isDestroyed,
        bool isNavigationAvailable,
        int allySeparation,
        bool orderCompatible,
        CoverInvalidationReason invalidationReason)
    {
        Point = point;
        PathCost = pathCost;
        OccupantCount = occupantCount;
        IsReservedByOther = isReservedByOther;
        ReservationHeld = reservationHeld;
        HasLineOfFire = hasLineOfFire;
        InGrenadeDanger = inGrenadeDanger;
        IsDestroyed = isDestroyed;
        IsNavigationAvailable = isNavigationAvailable;
        AllySeparation = allySeparation;
        OrderCompatible = orderCompatible;
        InvalidationReason = invalidationReason;
    }

    /// <summary>
    /// Gets the static tactical point record.
    /// </summary>
    public TacticalPointRecord Point { get; }

    /// <summary>
    /// Gets the integer path cost from the agent to the point.
    /// </summary>
    public int PathCost { get; }

    /// <summary>
    /// Gets the current occupants excluding the evaluating agent.
    /// </summary>
    public int OccupantCount { get; }

    /// <summary>
    /// Gets a value indicating whether another agent holds the reservation.
    /// </summary>
    public bool IsReservedByOther { get; }

    /// <summary>
    /// Gets a value indicating whether the evaluating agent holds the reservation.
    /// </summary>
    public bool ReservationHeld { get; }

    /// <summary>
    /// Gets a value indicating whether LOF to the threat focus exists.
    /// </summary>
    public bool HasLineOfFire { get; }

    /// <summary>
    /// Gets a value indicating whether the point is inside grenade danger.
    /// </summary>
    public bool InGrenadeDanger { get; }

    /// <summary>
    /// Gets a value indicating whether the point is destroyed.
    /// </summary>
    public bool IsDestroyed { get; }

    /// <summary>
    /// Gets a value indicating whether the linked nav node is walkable.
    /// </summary>
    public bool IsNavigationAvailable { get; }

    /// <summary>
    /// Gets the minimum Manhattan distance to nearby allies.
    /// </summary>
    public int AllySeparation { get; }

    /// <summary>
    /// Gets a value indicating whether the point matches the active squad order.
    /// </summary>
    public bool OrderCompatible { get; }

    /// <summary>
    /// Gets the precomputed invalidation reason, if any.
    /// </summary>
    public CoverInvalidationReason InvalidationReason { get; }
}

/// <summary>
/// Shared evaluation context for a bounded cover query.
/// </summary>
public readonly struct CoverEvaluationContext
{
    /// <summary>
    /// Initializes a cover evaluation context.
    /// </summary>
    /// <param name="agentPosition">Evaluating agent cell.</param>
    /// <param name="threatPosition">Primary threat cell.</param>
    /// <param name="threatFacing">Threat facing used for flank-angle scoring.</param>
    /// <param name="agentStance">Current agent stance.</param>
    /// <param name="activeOrder">Active squad order type biasing compatibility.</param>
    public CoverEvaluationContext(
        Int2 agentPosition,
        Int2 threatPosition,
        Direction8 threatFacing,
        AgentStance agentStance,
        SquadOrderType activeOrder)
    {
        AgentPosition = agentPosition;
        ThreatPosition = threatPosition;
        ThreatFacing = threatFacing;
        AgentStance = agentStance;
        ActiveOrder = activeOrder;
    }

    /// <summary>
    /// Gets the evaluating agent cell.
    /// </summary>
    public Int2 AgentPosition { get; }

    /// <summary>
    /// Gets the primary threat cell.
    /// </summary>
    public Int2 ThreatPosition { get; }

    /// <summary>
    /// Gets the threat facing used for flank-angle scoring.
    /// </summary>
    public Direction8 ThreatFacing { get; }

    /// <summary>
    /// Gets the current agent stance.
    /// </summary>
    public AgentStance AgentStance { get; }

    /// <summary>
    /// Gets the active squad order type biasing compatibility.
    /// </summary>
    public SquadOrderType ActiveOrder { get; }
}
