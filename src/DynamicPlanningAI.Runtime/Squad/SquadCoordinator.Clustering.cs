using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;

namespace DynamicPlanningAI.Runtime.Squad;

public sealed partial class SquadCoordinator
{
    [FrozenRuntimePath]
    private void MaybeRecluster(long tick, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        long elapsed = _lastReclusterTick == long.MinValue
            ? _options.ReclusterIntervalTicks
            : checked(tick - _lastReclusterTick);
        if (elapsed < _options.ReclusterIntervalTicks)
        {
            return;
        }

        Recluster(agents);
        _lastReclusterTick = tick;
    }

    [FrozenRuntimePath]
    private void Recluster(ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        PreserveOrdersAcrossRecluster();
        ClearMembershipTables();
        int sortedCount = BuildSortedAgentIndices(agents);
        for (int si = 0; si < sortedCount && si < AiHardLimits.MaximumAgents; si++)
        {
            int agentIndex = _sortScratch[si];
            SquadAgentSnapshot seed = agents[agentIndex];
            if (!seed.IsAlive || !seed.AgentId.IsValid)
            {
                continue;
            }

            if (seed.AgentId.Value >= AiHardLimits.MaximumAgents)
            {
                continue;
            }

            if (_agentSquadIndex[seed.AgentId.Value] >= 0)
            {
                continue;
            }

            if (!TryAllocateSquad(seed, out int squadIndex))
            {
                break;
            }

            AddMember(squadIndex, seed.AgentId, Int2.Zero);
            GrowSquadFromSeed(squadIndex, seed, agents, sortedCount);
        }
    }

    [FrozenRuntimePath]
    private void GrowSquadFromSeed(
        int squadIndex,
        in SquadAgentSnapshot seed,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        int sortedCount)
    {
        for (int si = 0; si < sortedCount && si < AiHardLimits.MaximumAgents; si++)
        {
            if (_squads[squadIndex].MemberCount >= _options.MaxAgentsPerSquad)
            {
                return;
            }

            SquadAgentSnapshot candidate = agents[_sortScratch[si]];
            if (!IsCompatibleCandidate(seed, candidate))
            {
                continue;
            }

            if (Int2.ManhattanDistance(seed.Position, candidate.Position) > _options.FormationProximity)
            {
                continue;
            }

            AddMember(squadIndex, candidate.AgentId, Int2.Zero);
        }
    }

    [FrozenRuntimePath]
    private static bool IsCompatibleCandidate(in SquadAgentSnapshot seed, in SquadAgentSnapshot candidate)
    {
        if (!candidate.IsAlive || !candidate.AgentId.IsValid)
        {
            return false;
        }

        if (candidate.AgentId.Value >= AiHardLimits.MaximumAgents)
        {
            return false;
        }

        if (candidate.TeamId != seed.TeamId || candidate.CompatibilityKey != seed.CompatibilityKey)
        {
            return false;
        }

        return candidate.AgentId != seed.AgentId;
    }

    [FrozenRuntimePath]
    private bool TryAllocateSquad(in SquadAgentSnapshot seed, out int squadIndex)
    {
        squadIndex = -1;
        if (_squadCount >= _options.MaxSquads)
        {
            return false;
        }

        squadIndex = _squadCount;
        ref SquadRecord squad = ref _squads[squadIndex];
        squad.Reset();
        squad.Id = SquadId.FromInt32(squadIndex);
        squad.IsActive = true;
        squad.TeamId = seed.TeamId;
        squad.CompatibilityKey = seed.CompatibilityKey;
        squad.FocusPosition = seed.Position;
        squad.MemberCount = 0;
        _squadCount = checked(_squadCount + 1);
        return true;
    }

    [FrozenRuntimePath]
    private void AddMember(int squadIndex, AgentId agentId, Int2 formationOffset)
    {
        if (!agentId.IsValid || agentId.Value >= AiHardLimits.MaximumAgents)
        {
            return;
        }

        if (_agentSquadIndex[agentId.Value] >= 0)
        {
            return;
        }

        ref SquadRecord squad = ref _squads[squadIndex];
        if (squad.MemberCount >= _options.MaxAgentsPerSquad)
        {
            return;
        }

        int slot = squad.MemberCount;
        int memberIndex = MemberIndex(squadIndex, slot);
        ref SquadMemberSlot member = ref _members[memberIndex];
        member.Clear();
        member.SlotIndex = (byte)slot;
        member.AgentId = agentId;
        member.IsOccupied = true;
        member.FormationOffset = formationOffset;
        squad.MemberCount = checked(squad.MemberCount + 1);
        _agentSquadIndex[agentId.Value] = squadIndex;
        _agentMemberIndex[agentId.Value] = slot;
    }

    [FrozenRuntimePath]
    private void ClearMembershipTables()
    {
        for (int i = 0; i < _squadCount && i < _options.MaxSquads; i++)
        {
            int memberCount = _squads[i].MemberCount;
            for (int s = 0; s < memberCount && s < _options.MaxAgentsPerSquad; s++)
            {
                _members[MemberIndex(i, s)].Clear();
            }

            _squads[i].Reset();
            _squads[i].Id = SquadId.FromInt32(i);
        }

        _squadCount = 0;
        ClearAgentMaps();
    }

    [FrozenRuntimePath]
    private void PreserveOrdersAcrossRecluster()
    {
        for (int i = 0; i < _orderCount && i < AiHardLimits.MaximumSquadOrders; i++)
        {
            if (!_orders[i].IsActive)
            {
                continue;
            }

            ReleaseOrderReservation(in _orders[i]);
            _orders[i] = _orders[i].WithStatus(SquadOrderStatus.Cancelled);
        }
    }

    [FrozenRuntimePath]
    private int BuildSortedAgentIndices(ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        int count = 0;
        for (int i = 0; i < agents.Length && i < AiHardLimits.MaximumAgents; i++)
        {
            if (!agents[i].AgentId.IsValid || !agents[i].IsAlive)
            {
                continue;
            }

            _sortScratch[count] = i;
            count = checked(count + 1);
        }

        // Insertion sort by AgentId for determinism (N ≤ MaximumAgents).
        for (int i = 1; i < count; i++)
        {
            int key = _sortScratch[i];
            int keyId = agents[key].AgentId.Value;
            int j = i - 1;
            while (j >= 0 && agents[_sortScratch[j]].AgentId.Value > keyId)
            {
                _sortScratch[j + 1] = _sortScratch[j];
                j = checked(j - 1);
            }

            _sortScratch[j + 1] = key;
        }

        return count;
    }
}
