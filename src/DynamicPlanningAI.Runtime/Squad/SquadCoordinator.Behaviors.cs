using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Ticks;

namespace DynamicPlanningAI.Runtime.Squad;

public sealed partial class SquadCoordinator
{
    [FrozenRuntimePath]
    private void TickSquad(
        int squadIndex,
        AiTick tick,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        ReadOnlySpan<SquadCoverCandidate> cover,
        ReadOnlySpan<SquadSearchSector> sectors)
    {
        ref SquadRecord squad = ref _squads[squadIndex];
        if (!squad.IsActive || squad.MemberCount < 1)
        {
            return;
        }

        UpdateFocus(squadIndex, agents);
        ObserveSurvivalOverrides(squadIndex, agents);

        if (squad.ActiveBehavior != SquadBehaviorType.None && tick.Sequence > squad.BehaviorExpiryTick)
        {
            EndBehavior(squadIndex);
        }

        if (squad.ActiveBehavior == SquadBehaviorType.None)
        {
            SelectBehavior(squadIndex, agents);
        }

        if (squad.ActiveBehavior == SquadBehaviorType.None)
        {
            return;
        }

        if (squad.OrdersIssuedThisBehavior == 0)
        {
            IssueBehaviorOrders(squadIndex, tick, agents, cover, sectors);
        }
    }

    [FrozenRuntimePath]
    private void UpdateFocus(int squadIndex, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        ref SquadRecord squad = ref _squads[squadIndex];
        int count = squad.MemberCount;
        if (count < 1)
        {
            return;
        }

        long sumX = 0L;
        long sumY = 0L;
        int used = 0;
        for (int s = 0; s < count && s < _options.MaxAgentsPerSquad; s++)
        {
            AgentId agentId = _members[MemberIndex(squadIndex, s)].AgentId;
            if (!TryFindAgentSnapshot(agents, agentId, out SquadAgentSnapshot snap))
            {
                continue;
            }

            sumX = checked(sumX + snap.Position.X);
            sumY = checked(sumY + snap.Position.Y);
            used = checked(used + 1);
        }

        if (used > 0)
        {
            squad.FocusPosition = new Int2((int)(sumX / used), (int)(sumY / used));
        }
    }

    [FrozenRuntimePath]
    private void ObserveSurvivalOverrides(int squadIndex, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        int count = _squads[squadIndex].MemberCount;
        for (int s = 0; s < count && s < _options.MaxAgentsPerSquad; s++)
        {
            ref SquadMemberSlot member = ref _members[MemberIndex(squadIndex, s)];
            if (!member.ActiveOrderId.IsValid)
            {
                continue;
            }

            if (!TryFindAgentSnapshot(agents, member.AgentId, out SquadAgentSnapshot snap))
            {
                continue;
            }

            if (!snap.IsInDanger)
            {
                continue;
            }

            _ = ReportOrderFailed(member.AgentId, member.ActiveOrderId, ActionFailureReason.DangerChanged);
        }
    }

    [FrozenRuntimePath]
    private void SelectBehavior(int squadIndex, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        SquadBehaviorType selected = ResolveBehavior(squadIndex, agents);
        if (selected == SquadBehaviorType.None)
        {
            return;
        }

        BeginBehavior(squadIndex, selected);
    }

    [FrozenRuntimePath]
    private SquadBehaviorType ResolveBehavior(int squadIndex, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        if (AnyMemberFlag(squadIndex, agents, MemberFlag.NeedsCover))
        {
            return SquadBehaviorType.GetToCover;
        }

        if (AnyMemberFlag(squadIndex, agents, MemberFlag.HasActiveThreat))
        {
            return SquadBehaviorType.AdvanceCover;
        }

        if (AnyMemberFlag(squadIndex, agents, MemberFlag.IsSeparated))
        {
            return SquadBehaviorType.Regroup;
        }

        if (_squads[squadIndex].MemberCount >= 2)
        {
            return SquadBehaviorType.HoldPosition;
        }

        return SquadBehaviorType.None;
    }

    private enum MemberFlag : byte
    {
        NeedsCover = 0,
        HasActiveThreat = 1,
        IsSeparated = 2,
    }

    [FrozenRuntimePath]
    private void BeginBehavior(int squadIndex, SquadBehaviorType behavior)
    {
        ref SquadRecord squad = ref _squads[squadIndex];
        squad.ActiveBehavior = behavior;
        squad.BehaviorStartedTick = _currentTick;
        squad.BehaviorExpiryTick = checked(_currentTick + _options.DefaultBehaviorDurationTicks);
        squad.OrdersIssuedThisBehavior = 0;
    }

    [FrozenRuntimePath]
    private void EndBehavior(int squadIndex)
    {
        CancelActiveOrdersForSquad(squadIndex);
        ref SquadRecord squad = ref _squads[squadIndex];
        squad.ActiveBehavior = SquadBehaviorType.None;
        squad.BehaviorStartedTick = 0L;
        squad.BehaviorExpiryTick = 0L;
        squad.OrdersIssuedThisBehavior = 0;
    }

    [FrozenRuntimePath]
    private void IssueBehaviorOrders(
        int squadIndex,
        AiTick tick,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        ReadOnlySpan<SquadCoverCandidate> cover,
        ReadOnlySpan<SquadSearchSector> sectors)
    {
        switch (_squads[squadIndex].ActiveBehavior)
        {
            case SquadBehaviorType.GetToCover:
                IssueGetToCover(squadIndex, tick, agents, cover);
                break;
            case SquadBehaviorType.AdvanceCover:
                IssueAdvanceCover(squadIndex, tick, agents, cover);
                break;
            case SquadBehaviorType.OrderlyAdvance:
                IssueOrderlyAdvance(squadIndex, tick, agents);
                break;
            case SquadBehaviorType.Search:
                IssueSearch(squadIndex, tick, agents, sectors);
                break;
            case SquadBehaviorType.Regroup:
                IssueRegroup(squadIndex, tick, agents);
                break;
            case SquadBehaviorType.HoldPosition:
                IssueHoldPosition(squadIndex, tick, agents);
                break;
            default:
                break;
        }
    }

    [FrozenRuntimePath]
    private bool AnyMemberFlag(
        int squadIndex,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        MemberFlag flag)
    {
        int count = _squads[squadIndex].MemberCount;
        for (int s = 0; s < count && s < _options.MaxAgentsPerSquad; s++)
        {
            AgentId agentId = _members[MemberIndex(squadIndex, s)].AgentId;
            if (!TryFindAgentSnapshot(agents, agentId, out SquadAgentSnapshot snap))
            {
                continue;
            }

            if (MatchesFlag(in snap, flag))
            {
                return true;
            }
        }

        return false;
    }

    [FrozenRuntimePath]
    private static bool MatchesFlag(in SquadAgentSnapshot snap, MemberFlag flag)
    {
        return flag switch
        {
            MemberFlag.NeedsCover => snap.NeedsCover,
            MemberFlag.HasActiveThreat => snap.HasActiveThreat,
            MemberFlag.IsSeparated => snap.IsSeparated,
            _ => false,
        };
    }

    [FrozenRuntimePath]
    private static bool TryFindAgentSnapshot(
        ReadOnlySpan<SquadAgentSnapshot> agents,
        AgentId agentId,
        out SquadAgentSnapshot snapshot)
    {
        for (int i = 0; i < agents.Length && i < AiHardLimits.MaximumAgents; i++)
        {
            if (agents[i].AgentId != agentId)
            {
                continue;
            }

            snapshot = agents[i];
            return true;
        }

        snapshot = default;
        return false;
    }
}
