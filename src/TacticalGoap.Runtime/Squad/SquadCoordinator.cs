using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Diagnostics;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Communication;

namespace TacticalGoap.Runtime.Squad;

/// <summary>
/// Bounded global squad coordinator: clustering, one behavior per squad, and order issuance.
/// </summary>
/// <remarks>
/// Agents are never puppeted. Orders activate <see cref="FollowSquadOrderGoalData"/> only.
/// Clustering is O(N²) with N ≤ <see cref="AiHardLimits.MaximumAgents"/> and runs on a
/// recluster interval rather than every tick.
/// </remarks>
public sealed partial class SquadCoordinator
{
    private readonly SquadCoordinatorOptions _options;
    private readonly SquadRecord[] _squads;
    private readonly SquadMemberSlot[] _members;
    private readonly SquadOrder[] _orders;
    private readonly int[] _agentSquadIndex;
    private readonly int[] _agentMemberIndex;
    private readonly int[] _sortScratch;
    private readonly int[] _candidateScratch;
    private readonly int[] _reservedCoverScratch;
    private int _squadCount;
    private int _orderCount;
    private int _nextOrderRawId;
    private long _lastReclusterTick;
    private long _currentTick;
    private ISquadReservationGateway _reservations;
    private CommunicationArbiter? _communication;
    private IRuntimeTraceSink? _trace;

    /// <summary>
    /// Initializes a coordinator with validated options.
    /// </summary>
    /// <param name="options">Coordinator options; cloned into validated fields.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when options fail validation.</exception>
    public SquadCoordinator(SquadCoordinatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Validate())
        {
            throw new ArgumentException("SquadCoordinatorOptions failed validation.", nameof(options));
        }

        _options = options;
        _squads = new SquadRecord[options.MaxSquads];
        _members = new SquadMemberSlot[checked(options.MaxSquads * options.MaxAgentsPerSquad)];
        _orders = new SquadOrder[AiHardLimits.MaximumSquadOrders];
        _agentSquadIndex = new int[AiHardLimits.MaximumAgents];
        _agentMemberIndex = new int[AiHardLimits.MaximumAgents];
        _sortScratch = new int[AiHardLimits.MaximumAgents];
        _candidateScratch = new int[AiHardLimits.MaximumAgentsPerSquad];
        _reservedCoverScratch = new int[AiHardLimits.MaximumCoverCandidates];
        _reservations = new LocalSquadReservationGateway();
        _squadCount = 0;
        _orderCount = 0;
        _nextOrderRawId = 0;
        _lastReclusterTick = long.MinValue;
        _currentTick = 0L;
        ClearAgentMaps();

        for (int i = 0; i < _squads.Length; i++)
        {
            _squads[i] = new SquadRecord(SquadId.FromInt32(i));
            for (int s = 0; s < options.MaxAgentsPerSquad; s++)
            {
                _members[MemberIndex(i, s)] = new SquadMemberSlot((byte)s);
            }
        }
    }

    /// <summary>Gets the number of active squads.</summary>
    public int SquadCount => _squadCount;

    /// <summary>Gets the number of live order table entries.</summary>
    public int OrderCount => _orderCount;

    /// <summary>
    /// Binds optional reservation, communication, and diagnostic sinks.
    /// </summary>
    /// <param name="reservations">Reservation gateway; null keeps the local gateway.</param>
    /// <param name="communication">Optional communication arbiter for intent emission.</param>
    /// <param name="trace">Optional diagnostic sink.</param>
    public void BindServices(
        ISquadReservationGateway? reservations,
        CommunicationArbiter? communication,
        IRuntimeTraceSink? trace)
    {
        if (reservations is not null)
        {
            _reservations = reservations;
        }

        _communication = communication;
        _trace = trace;
    }

    /// <summary>
    /// Advances squad coordination for one tick.
    /// </summary>
    /// <param name="tick">Host tick stamp.</param>
    /// <param name="agents">Agent snapshots visible this tick.</param>
    /// <param name="cover">Cover candidates visible this tick.</param>
    /// <param name="sectors">Search sectors visible this tick.</param>
    /// <returns>Success or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Tick(
        AiTick tick,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        ReadOnlySpan<SquadCoverCandidate> cover,
        ReadOnlySpan<SquadSearchSector> sectors)
    {
        if (tick.Sequence < 0 || agents.Length > AiHardLimits.MaximumAgents)
        {
            return OperationStatus.InvalidArgument;
        }

        if (cover.Length > AiHardLimits.MaximumTacticalPoints ||
            sectors.Length > AiHardLimits.MaximumSearchSectors)
        {
            return OperationStatus.InvalidArgument;
        }

        _currentTick = tick.Sequence;
        MaybeRecluster(tick.Sequence, agents);
        ExpireOrders(tick.Sequence);
        for (int i = 0; i < _squadCount && i < _options.MaxSquads; i++)
        {
            TickSquad(i, tick, agents, cover, sectors);
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Tries to read a squad record by identifier.
    /// </summary>
    /// <param name="squadId">Squad identifier.</param>
    /// <param name="record">Receives the record when found.</param>
    /// <returns><see langword="true"/> when the squad is active.</returns>
    public bool TryGetSquad(SquadId squadId, out SquadRecord record)
    {
        if (!TryGetSquadIndex(squadId, out int index))
        {
            record = default;
            return false;
        }

        record = _squads[index];
        return record.IsActive;
    }

    /// <summary>
    /// Copies occupied member slots for a squad into a caller buffer.
    /// </summary>
    /// <param name="squadId">Squad identifier.</param>
    /// <param name="destination">Caller-owned destination.</param>
    /// <param name="written">Receives the number of slots written.</param>
    /// <returns>Success or not-found / capacity status.</returns>
    public OperationStatus CopyMembers(SquadId squadId, Span<SquadMemberSlot> destination, out int written)
    {
        written = 0;
        if (!TryGetSquadIndex(squadId, out int squadIndex))
        {
            return OperationStatus.NotFound;
        }

        int count = _squads[squadIndex].MemberCount;
        if (destination.Length < count)
        {
            return OperationStatus.CapacityExceeded;
        }

        for (int s = 0; s < count && s < _options.MaxAgentsPerSquad; s++)
        {
            destination[s] = _members[MemberIndex(squadIndex, s)];
            written = checked(written + 1);
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Tries to read the active order for an agent.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="order">Receives the active order when present.</param>
    /// <returns><see langword="true"/> when an active order exists.</returns>
    public bool TryGetActiveOrder(AgentId agentId, out SquadOrder order)
    {
        order = default;
        if (!agentId.IsValid || agentId.Value >= AiHardLimits.MaximumAgents)
        {
            return false;
        }

        int squadIndex = _agentSquadIndex[agentId.Value];
        int memberIndex = _agentMemberIndex[agentId.Value];
        if (squadIndex < 0 || memberIndex < 0)
        {
            return false;
        }

        OrderId orderId = _members[MemberIndex(squadIndex, memberIndex)].ActiveOrderId;
        return TryFindOrder(orderId, out order) && order.IsActive;
    }

    /// <summary>
    /// Tries to read an order by identifier, including terminal states.
    /// </summary>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="order">Receives the order when found.</param>
    /// <returns><see langword="true"/> when the order exists in the table.</returns>
    public bool TryGetOrder(OrderId orderId, out SquadOrder order) => TryFindOrder(orderId, out order);

    /// <summary>
    /// Tries to read FollowSquadOrderGoal activation data for an agent.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="data">Receives goal data when an active order exists.</param>
    /// <returns><see langword="true"/> when goal data is available.</returns>
    public bool TryGetFollowSquadOrderGoalData(AgentId agentId, out FollowSquadOrderGoalData data)
    {
        if (!TryGetActiveOrder(agentId, out SquadOrder order))
        {
            data = default;
            return false;
        }

        data = order.ToGoalData();
        return true;
    }

    /// <summary>
    /// Requests a specific behavior for a squad (host / configuration driven).
    /// </summary>
    /// <param name="squadId">Target squad.</param>
    /// <param name="behavior">Behavior to activate; must not be <see cref="SquadBehaviorType.None"/>.</param>
    /// <returns>Success or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus RequestBehavior(SquadId squadId, SquadBehaviorType behavior)
    {
        if (behavior == SquadBehaviorType.None)
        {
            return OperationStatus.InvalidArgument;
        }

        if (!TryGetSquadIndex(squadId, out int squadIndex))
        {
            return OperationStatus.NotFound;
        }

        if (_squads[squadIndex].ActiveBehavior != SquadBehaviorType.None)
        {
            EndBehavior(squadIndex);
        }

        BeginBehavior(squadIndex, behavior);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Records that an agent acknowledged an issued order.
    /// </summary>
    /// <param name="agentId">Acknowledging agent.</param>
    /// <param name="orderId">Order identifier.</param>
    /// <returns>Success or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus AcknowledgeOrder(AgentId agentId, OrderId orderId)
    {
        if (!TryFindOrderIndex(orderId, out int index))
        {
            return OperationStatus.NotFound;
        }

        SquadOrder order = _orders[index];
        if (order.Assignee != agentId || order.Status != SquadOrderStatus.Issued)
        {
            return OperationStatus.Conflict;
        }

        _orders[index] = order.WithStatus(SquadOrderStatus.Acknowledged);
        EmitIntent(agentId, order.SquadId, CommunicationIntentType.OrderAcknowledged, EntityId.Invalid);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Records successful order completion.
    /// </summary>
    /// <param name="agentId">Completing agent.</param>
    /// <param name="orderId">Order identifier.</param>
    /// <returns>Success or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus ReportOrderSucceeded(AgentId agentId, OrderId orderId)
    {
        return CompleteOrder(agentId, orderId, SquadOrderStatus.Succeeded, survivalOverride: false);
    }

    /// <summary>
    /// Records order failure. Danger-driven failures set survival override.
    /// </summary>
    /// <param name="agentId">Failing agent.</param>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="reason">Failure reason from execution.</param>
    /// <returns>Success or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus ReportOrderFailed(AgentId agentId, OrderId orderId, ActionFailureReason reason)
    {
        bool survival = reason == ActionFailureReason.DangerChanged;
        return CompleteOrder(agentId, orderId, SquadOrderStatus.Failed, survival);
    }

    private void ClearAgentMaps()
    {
        for (int i = 0; i < AiHardLimits.MaximumAgents; i++)
        {
            _agentSquadIndex[i] = -1;
            _agentMemberIndex[i] = -1;
        }
    }

    private int MemberIndex(int squadIndex, int slot) =>
        checked((squadIndex * _options.MaxAgentsPerSquad) + slot);

    private bool TryGetSquadIndex(SquadId squadId, out int index)
    {
        index = -1;
        if (!squadId.IsValid || squadId.Value >= _options.MaxSquads)
        {
            return false;
        }

        index = squadId.Value;
        return _squads[index].IsActive && index < _squadCount;
    }
}
