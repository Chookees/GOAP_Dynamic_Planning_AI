using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Discrete squad order assigned to one agent. Agents satisfy orders via GOAP,
/// never by direct puppeting.
/// </summary>
/// <remarks>
/// Issuing an order activates <see cref="FollowSquadOrderGoalData"/> for the
/// assignee. Facts <c>SquadOrderAvailable</c> / <c>SquadOrderSatisfied</c> are
/// mirrored by the goal / execution layers.
/// </remarks>
public readonly struct SquadOrder
{
    /// <summary>
    /// Initializes a squad order with all specification fields.
    /// </summary>
    /// <param name="orderId">Stable order identifier.</param>
    /// <param name="squadId">Owning squad.</param>
    /// <param name="assignee">Assigned agent.</param>
    /// <param name="orderType">Order kind.</param>
    /// <param name="role">Slot role that produced the order.</param>
    /// <param name="pointId">Optional tactical point; may be invalid.</param>
    /// <param name="sectorId">Optional search sector; may be invalid.</param>
    /// <param name="issuedTick">Tick when the order was issued.</param>
    /// <param name="expiryTick">Tick after which the order times out.</param>
    /// <param name="status">Lifecycle status.</param>
    /// <param name="reservationResourceId">Reserved resource id; negative when none.</param>
    /// <param name="survivalOverride">True when the agent refused due to danger.</param>
    public SquadOrder(
        OrderId orderId,
        SquadId squadId,
        AgentId assignee,
        SquadOrderType orderType,
        SquadSlotRole role,
        TacticalPointId pointId,
        SearchSectorId sectorId,
        long issuedTick,
        long expiryTick,
        SquadOrderStatus status,
        int reservationResourceId,
        bool survivalOverride)
    {
        OrderId = orderId;
        SquadId = squadId;
        Assignee = assignee;
        OrderType = orderType;
        Role = role;
        PointId = pointId;
        SectorId = sectorId;
        IssuedTick = issuedTick;
        ExpiryTick = expiryTick;
        Status = status;
        ReservationResourceId = reservationResourceId;
        SurvivalOverride = survivalOverride;
    }

    /// <summary>Gets the stable order identifier.</summary>
    public OrderId OrderId { get; }

    /// <summary>Gets the owning squad.</summary>
    public SquadId SquadId { get; }

    /// <summary>Gets the assigned agent.</summary>
    public AgentId Assignee { get; }

    /// <summary>Gets the order kind.</summary>
    public SquadOrderType OrderType { get; }

    /// <summary>Gets the slot role that produced the order.</summary>
    public SquadSlotRole Role { get; }

    /// <summary>Gets the optional tactical point.</summary>
    public TacticalPointId PointId { get; }

    /// <summary>Gets the optional search sector.</summary>
    public SearchSectorId SectorId { get; }

    /// <summary>Gets the issue tick sequence.</summary>
    public long IssuedTick { get; }

    /// <summary>Gets the expiry tick sequence.</summary>
    public long ExpiryTick { get; }

    /// <summary>Gets the lifecycle status.</summary>
    public SquadOrderStatus Status { get; }

    /// <summary>Gets the reserved resource id; negative when none.</summary>
    public int ReservationResourceId { get; }

    /// <summary>Gets a value indicating survival-driven refusal.</summary>
    public bool SurvivalOverride { get; }

    /// <summary>Gets a value indicating whether the order is live.</summary>
    public bool IsActive =>
        Status is SquadOrderStatus.Issued
            or SquadOrderStatus.Acknowledged
            or SquadOrderStatus.InProgress;

    /// <summary>
    /// Builds goal activation data for <c>FollowSquadOrderGoal</c>.
    /// </summary>
    /// <returns>Goal data mirroring this order.</returns>
    public FollowSquadOrderGoalData ToGoalData()
    {
        return new FollowSquadOrderGoalData(
            OrderId,
            SquadId,
            OrderType,
            PointId,
            SectorId,
            ExpiryTick);
    }

    /// <summary>
    /// Returns a copy with an updated status.
    /// </summary>
    /// <param name="status">New status.</param>
    /// <param name="survivalOverride">Optional survival override flag.</param>
    /// <returns>Updated order value.</returns>
    public SquadOrder WithStatus(SquadOrderStatus status, bool survivalOverride = false)
    {
        return new SquadOrder(
            OrderId,
            SquadId,
            Assignee,
            OrderType,
            Role,
            PointId,
            SectorId,
            IssuedTick,
            ExpiryTick,
            status,
            ReservationResourceId,
            survivalOverride);
    }
}
