using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Runtime.Squad;

/// <summary>
/// Payload that activates a <c>FollowSquadOrderGoal</c> without puppeting the agent.
/// </summary>
/// <remarks>
/// The goal arbitrator consumes this data to bias desire facts
/// (<c>SquadOrderAvailable</c>). Concrete action sequences remain planned by GOAP.
/// </remarks>
public readonly struct FollowSquadOrderGoalData
{
    /// <summary>
    /// Initializes goal activation data from an order.
    /// </summary>
    /// <param name="orderId">Active order identifier.</param>
    /// <param name="squadId">Owning squad.</param>
    /// <param name="orderType">Order kind driving desire facts.</param>
    /// <param name="pointId">Optional tactical point.</param>
    /// <param name="sectorId">Optional search sector.</param>
    /// <param name="expiryTick">Order expiry tick.</param>
    public FollowSquadOrderGoalData(
        OrderId orderId,
        SquadId squadId,
        SquadOrderType orderType,
        TacticalPointId pointId,
        SearchSectorId sectorId,
        long expiryTick)
    {
        OrderId = orderId;
        SquadId = squadId;
        OrderType = orderType;
        PointId = pointId;
        SectorId = sectorId;
        ExpiryTick = expiryTick;
    }

    /// <summary>Gets the active order identifier.</summary>
    public OrderId OrderId { get; }

    /// <summary>Gets the owning squad.</summary>
    public SquadId SquadId { get; }

    /// <summary>Gets the order kind.</summary>
    public SquadOrderType OrderType { get; }

    /// <summary>Gets the optional tactical point.</summary>
    public TacticalPointId PointId { get; }

    /// <summary>Gets the optional search sector.</summary>
    public SearchSectorId SectorId { get; }

    /// <summary>Gets the order expiry tick.</summary>
    public long ExpiryTick { get; }

    /// <summary>Gets a value indicating whether the payload references a valid order.</summary>
    public bool IsValid => OrderId.IsValid && SquadId.IsValid;
}
