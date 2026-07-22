using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Diagnostics;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Abstractions.Ticks;
using DynamicPlanningAI.Runtime.Communication;

namespace DynamicPlanningAI.Runtime.Squad;

public sealed partial class SquadCoordinator
{
    [FrozenRuntimePath]
    private OperationStatus CompleteOrder(
        AgentId agentId,
        OrderId orderId,
        SquadOrderStatus terminalStatus,
        bool survivalOverride)
    {
        if (!TryFindOrderIndex(orderId, out int index))
        {
            return OperationStatus.NotFound;
        }

        SquadOrder order = _orders[index];
        if (order.Assignee != agentId || !order.IsActive)
        {
            return OperationStatus.Conflict;
        }

        ReleaseOrderReservation(in order);
        ClearMemberOrder(order.Assignee);
        _orders[index] = order.WithStatus(terminalStatus, survivalOverride);

        if (terminalStatus == SquadOrderStatus.Failed)
        {
            EmitIntent(agentId, order.SquadId, CommunicationIntentType.OrderFailed, EntityId.Invalid);
        }

        WriteTrace(agentId, order.SquadId, TraceEventCode.OrderCompleted, orderId.Value, (int)terminalStatus);
        return OperationStatus.Success;
    }

    [FrozenRuntimePath]
    private void ExpireOrders(long tick)
    {
        for (int i = 0; i < _orderCount && i < AiHardLimits.MaximumSquadOrders; i++)
        {
            SquadOrder order = _orders[i];
            if (!order.IsActive)
            {
                continue;
            }

            if (tick <= order.ExpiryTick)
            {
                continue;
            }

            ReleaseOrderReservation(in order);
            ClearMemberOrder(order.Assignee);
            _orders[i] = order.WithStatus(SquadOrderStatus.TimedOut);
            EmitIntent(order.Assignee, order.SquadId, CommunicationIntentType.OrderFailed, EntityId.Invalid);
        }
    }

    [FrozenRuntimePath]
    private bool TryIssueOrder(
        int squadIndex,
        AgentId assignee,
        SquadOrderType orderType,
        SquadSlotRole role,
        TacticalPointId pointId,
        SearchSectorId sectorId,
        int reservationResourceId,
        AiTick tick,
        out OrderId orderId)
    {
        orderId = OrderId.Invalid;
        if (_orderCount >= AiHardLimits.MaximumSquadOrders)
        {
            return false;
        }

        if (!assignee.IsValid || assignee.Value >= AiHardLimits.MaximumAgents)
        {
            return false;
        }

        int memberSlot = _agentMemberIndex[assignee.Value];
        if (_agentSquadIndex[assignee.Value] != squadIndex || memberSlot < 0)
        {
            return false;
        }

        if (reservationResourceId >= 0)
        {
            ReservationResult reserved = _reservations.TryReserve(assignee, reservationResourceId, tick.Sequence);
            if (!reserved.IsOwned)
            {
                return false;
            }
        }

        orderId = OrderId.FromInt32(_nextOrderRawId);
        _nextOrderRawId = checked(_nextOrderRawId + 1);
        long expiry = checked(tick.Sequence + _options.DefaultOrderDurationTicks);
        SquadOrder order = new(
            orderId,
            _squads[squadIndex].Id,
            assignee,
            orderType,
            role,
            pointId,
            sectorId,
            tick.Sequence,
            expiry,
            SquadOrderStatus.Issued,
            reservationResourceId,
            survivalOverride: false);

        _orders[_orderCount] = order;
        _orderCount = checked(_orderCount + 1);

        ref SquadMemberSlot member = ref _members[MemberIndex(squadIndex, memberSlot)];
        member.ActiveOrderId = orderId;
        member.Role = role;
        _squads[squadIndex].OrdersIssuedThisBehavior =
            checked(_squads[squadIndex].OrdersIssuedThisBehavior + 1);

        WriteTrace(assignee, order.SquadId, TraceEventCode.OrderIssued, orderId.Value, (int)orderType);
        return true;
    }

    [FrozenRuntimePath]
    private void ReleaseOrderReservation(in SquadOrder order)
    {
        if (order.ReservationResourceId < 0)
        {
            return;
        }

        _ = _reservations.Release(order.Assignee, order.ReservationResourceId);
    }

    [FrozenRuntimePath]
    private void ClearMemberOrder(AgentId agentId)
    {
        if (!agentId.IsValid || agentId.Value >= AiHardLimits.MaximumAgents)
        {
            return;
        }

        int squadIndex = _agentSquadIndex[agentId.Value];
        int memberSlot = _agentMemberIndex[agentId.Value];
        if (squadIndex < 0 || memberSlot < 0)
        {
            return;
        }

        ref SquadMemberSlot member = ref _members[MemberIndex(squadIndex, memberSlot)];
        member.ActiveOrderId = OrderId.Invalid;
        member.Role = SquadSlotRole.None;
    }

    [FrozenRuntimePath]
    private void CancelActiveOrdersForSquad(int squadIndex)
    {
        SquadId squadId = _squads[squadIndex].Id;
        for (int i = 0; i < _orderCount && i < AiHardLimits.MaximumSquadOrders; i++)
        {
            if (_orders[i].SquadId != squadId || !_orders[i].IsActive)
            {
                continue;
            }

            ReleaseOrderReservation(in _orders[i]);
            ClearMemberOrder(_orders[i].Assignee);
            _orders[i] = _orders[i].WithStatus(SquadOrderStatus.Cancelled);
        }
    }

    private bool TryFindOrder(OrderId orderId, out SquadOrder order)
    {
        if (TryFindOrderIndex(orderId, out int index))
        {
            order = _orders[index];
            return true;
        }

        order = default;
        return false;
    }

    private bool TryFindOrderIndex(OrderId orderId, out int index)
    {
        index = -1;
        if (!orderId.IsValid)
        {
            return false;
        }

        for (int i = 0; i < _orderCount && i < AiHardLimits.MaximumSquadOrders; i++)
        {
            if (_orders[i].OrderId != orderId)
            {
                continue;
            }

            index = i;
            return true;
        }

        return false;
    }

    [FrozenRuntimePath]
    private void EmitIntent(
        AgentId speaker,
        SquadId squad,
        CommunicationIntentType intent,
        EntityId related)
    {
        if (_communication is null || !speaker.IsValid)
        {
            return;
        }

        _communication.SynchronizeTick(_currentTick);
        _ = _communication.Request(
            speaker,
            intent,
            related,
            squad,
            priority: 0,
            expiryTick: checked(_currentTick + 8));
    }

    [FrozenRuntimePath]
    private void WriteTrace(AgentId agent, SquadId squad, TraceEventCode code, int primary, int secondary)
    {
        if (_trace is null)
        {
            return;
        }

        AiTick tick = new(_currentTick, 1);
        TraceRecord record = new(
            tick,
            agent,
            squad,
            DiagnosticSubsystem.Squad,
            (int)code,
            primary,
            secondary,
            valueA: 0,
            valueB: 0,
            statusCode: 0);
        _trace.Write(in record);
    }
}
