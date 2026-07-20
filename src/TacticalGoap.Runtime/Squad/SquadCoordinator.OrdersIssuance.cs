using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Ticks;

namespace TacticalGoap.Runtime.Squad;

public sealed partial class SquadCoordinator
{
    [FrozenRuntimePath]
    private void IssueGetToCover(
        int squadIndex,
        AiTick tick,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        ReadOnlySpan<SquadCoverCandidate> cover)
    {
        int memberCount = CollectMembersWithoutOrders(squadIndex);
        AssignCoverOrders(squadIndex, tick, agents, cover, memberCount, includeSuppressor: true);
    }

    [FrozenRuntimePath]
    private void IssueAdvanceCover(
        int squadIndex,
        AiTick tick,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        ReadOnlySpan<SquadCoverCandidate> cover)
    {
        int memberCount = CollectMembersWithoutOrders(squadIndex);
        if (memberCount < 1)
        {
            return;
        }

        int suppressorLocal = SelectBestSuppressor(squadIndex, agents, memberCount);
        if (suppressorLocal >= 0)
        {
            AgentId suppressor = _members[MemberIndex(squadIndex, _candidateScratch[suppressorLocal])].AgentId;
            _ = TryIssueOrder(
                squadIndex,
                suppressor,
                SquadOrderType.ProvideSuppression,
                SquadSlotRole.Suppressor,
                TacticalPointId.Invalid,
                SearchSectorId.Invalid,
                reservationResourceId: -1,
                tick,
                out _);
        }

        AssignCoverOrders(squadIndex, tick, agents, cover, CollectMembersWithoutOrders(squadIndex), includeSuppressor: false);
    }

    [FrozenRuntimePath]
    private void IssueOrderlyAdvance(int squadIndex, AiTick tick, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        int memberCount = CollectMembersWithoutOrders(squadIndex);
        SortCandidatesByAgentId(squadIndex, memberCount);
        Int2 focus = _squads[squadIndex].FocusPosition;
        for (int i = 0; i < memberCount && i < _options.MaxAgentsPerSquad; i++)
        {
            int slot = _candidateScratch[i];
            AgentId agentId = _members[MemberIndex(squadIndex, slot)].AgentId;
            Int2 offset = new(i, 0);
            _members[MemberIndex(squadIndex, slot)].FormationOffset = offset;
            _ = agents;
            _ = focus;
            _ = TryIssueOrder(
                squadIndex,
                agentId,
                SquadOrderType.FollowFormation,
                SquadSlotRole.Formation,
                TacticalPointId.Invalid,
                SearchSectorId.Invalid,
                reservationResourceId: -1,
                tick,
                out _);
        }
    }

    [FrozenRuntimePath]
    private void IssueSearch(
        int squadIndex,
        AiTick tick,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        ReadOnlySpan<SquadSearchSector> sectors)
    {
        int memberCount = CollectMembersWithoutOrders(squadIndex);
        SortCandidatesByAgentId(squadIndex, memberCount);
        int sectorCursor = 0;
        for (int i = 0; i < memberCount && i < _options.MaxAgentsPerSquad; i++)
        {
            SearchSectorId sectorId = SearchSectorId.Invalid;
            while (sectorCursor < sectors.Length && sectorCursor < AiHardLimits.MaximumSearchSectors)
            {
                if (!sectors[sectorCursor].IsClear)
                {
                    sectorId = sectors[sectorCursor].SectorId;
                    sectorCursor = checked(sectorCursor + 1);
                    break;
                }

                sectorCursor = checked(sectorCursor + 1);
            }

            AgentId agentId = _members[MemberIndex(squadIndex, _candidateScratch[i])].AgentId;
            _ = agents;
            _ = TryIssueOrder(
                squadIndex,
                agentId,
                SquadOrderType.SearchSector,
                SquadSlotRole.Searcher,
                TacticalPointId.Invalid,
                sectorId,
                reservationResourceId: -1,
                tick,
                out _);
        }
    }

    [FrozenRuntimePath]
    private void IssueRegroup(int squadIndex, AiTick tick, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        int memberCount = CollectMembersWithoutOrders(squadIndex);
        SortCandidatesByAgentId(squadIndex, memberCount);
        for (int i = 0; i < memberCount && i < _options.MaxAgentsPerSquad; i++)
        {
            AgentId agentId = _members[MemberIndex(squadIndex, _candidateScratch[i])].AgentId;
            _ = agents;
            _ = TryIssueOrder(
                squadIndex,
                agentId,
                SquadOrderType.Regroup,
                SquadSlotRole.Rally,
                TacticalPointId.Invalid,
                SearchSectorId.Invalid,
                reservationResourceId: -1,
                tick,
                out _);
        }
    }

    [FrozenRuntimePath]
    private void IssueHoldPosition(int squadIndex, AiTick tick, ReadOnlySpan<SquadAgentSnapshot> agents)
    {
        int memberCount = CollectMembersWithoutOrders(squadIndex);
        SortCandidatesByAgentId(squadIndex, memberCount);
        for (int i = 0; i < memberCount && i < _options.MaxAgentsPerSquad; i++)
        {
            AgentId agentId = _members[MemberIndex(squadIndex, _candidateScratch[i])].AgentId;
            _ = agents;
            _ = TryIssueOrder(
                squadIndex,
                agentId,
                SquadOrderType.Hold,
                SquadSlotRole.Holder,
                TacticalPointId.Invalid,
                SearchSectorId.Invalid,
                reservationResourceId: -1,
                tick,
                out _);
        }
    }

    [FrozenRuntimePath]
    private void AssignCoverOrders(
        int squadIndex,
        AiTick tick,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        ReadOnlySpan<SquadCoverCandidate> cover,
        int memberCount,
        bool includeSuppressor)
    {
        SortCandidatesByAgentId(squadIndex, memberCount);
        bool suppressorAssigned = !includeSuppressor;
        int reservedCount = 0;

        for (int i = 0; i < memberCount && i < _options.MaxAgentsPerSquad; i++)
        {
            int slot = _candidateScratch[i];
            AgentId agentId = _members[MemberIndex(squadIndex, slot)].AgentId;
            if (!TryFindAgentSnapshot(agents, agentId, out SquadAgentSnapshot snap))
            {
                continue;
            }

            if (TryGetActiveOrder(agentId, out _))
            {
                continue;
            }

            if (!suppressorAssigned && snap.CanSuppress && snap.HasActiveThreat)
            {
                suppressorAssigned = true;
                _ = TryIssueOrder(
                    squadIndex,
                    agentId,
                    SquadOrderType.ProvideSuppression,
                    SquadSlotRole.Suppressor,
                    TacticalPointId.Invalid,
                    SearchSectorId.Invalid,
                    reservationResourceId: -1,
                    tick,
                    out _);
                continue;
            }

            if (!TrySelectBestCover(snap.Position, cover, reservedCount, out SquadCoverCandidate best))
            {
                continue;
            }

            if (!TryIssueOrder(
                    squadIndex,
                    agentId,
                    SquadOrderType.MoveToCover,
                    SquadSlotRole.Mover,
                    best.PointId,
                    SearchSectorId.Invalid,
                    reservationResourceId: best.PointId.Value,
                    tick,
                    out _))
            {
                continue;
            }

            if (reservedCount < AiHardLimits.MaximumCoverCandidates)
            {
                _reservedCoverScratch[reservedCount] = best.PointId.Value;
                reservedCount = checked(reservedCount + 1);
            }
        }
    }

    [FrozenRuntimePath]
    private bool TrySelectBestCover(
        Int2 agentPosition,
        ReadOnlySpan<SquadCoverCandidate> cover,
        int reservedCount,
        out SquadCoverCandidate best)
    {
        best = default;
        int bestScore = int.MinValue;
        int bestPoint = int.MaxValue;
        bool found = false;
        for (int i = 0; i < cover.Length && i < AiHardLimits.MaximumCoverCandidates; i++)
        {
            if (!cover[i].PointId.IsValid || cover[i].Capacity < 1)
            {
                continue;
            }

            if (IsCoverReserved(cover[i].PointId.Value, reservedCount))
            {
                continue;
            }

            int distance = Int2.ManhattanDistance(agentPosition, cover[i].Position);
            int score = checked(-distance);
            int pointValue = cover[i].PointId.Value;
            if (!found || score > bestScore || (score == bestScore && pointValue < bestPoint))
            {
                best = cover[i];
                bestScore = score;
                bestPoint = pointValue;
                found = true;
            }
        }

        return found;
    }

    [FrozenRuntimePath]
    private bool IsCoverReserved(int pointValue, int reservedCount)
    {
        for (int i = 0; i < reservedCount && i < AiHardLimits.MaximumCoverCandidates; i++)
        {
            if (_reservedCoverScratch[i] == pointValue)
            {
                return true;
            }
        }

        return false;
    }

    [FrozenRuntimePath]
    private int CollectMembersWithoutOrders(int squadIndex)
    {
        int count = 0;
        int memberCount = _squads[squadIndex].MemberCount;
        for (int s = 0; s < memberCount && s < _options.MaxAgentsPerSquad; s++)
        {
            if (_members[MemberIndex(squadIndex, s)].ActiveOrderId.IsValid)
            {
                continue;
            }

            _candidateScratch[count] = s;
            count = checked(count + 1);
        }

        return count;
    }

    [FrozenRuntimePath]
    private void SortCandidatesByAgentId(int squadIndex, int candidateCount)
    {
        for (int i = 1; i < candidateCount; i++)
        {
            int keySlot = _candidateScratch[i];
            int keyId = _members[MemberIndex(squadIndex, keySlot)].AgentId.Value;
            int j = i - 1;
            while (j >= 0)
            {
                int otherId = _members[MemberIndex(squadIndex, _candidateScratch[j])].AgentId.Value;
                if (otherId <= keyId)
                {
                    break;
                }

                _candidateScratch[j + 1] = _candidateScratch[j];
                j = checked(j - 1);
            }

            _candidateScratch[j + 1] = keySlot;
        }
    }

    [FrozenRuntimePath]
    private int SelectBestSuppressor(
        int squadIndex,
        ReadOnlySpan<SquadAgentSnapshot> agents,
        int candidateCount)
    {
        int bestLocal = -1;
        int bestScore = int.MinValue;
        int bestAgentId = int.MaxValue;
        for (int i = 0; i < candidateCount && i < _options.MaxAgentsPerSquad; i++)
        {
            int slot = _candidateScratch[i];
            AgentId agentId = _members[MemberIndex(squadIndex, slot)].AgentId;
            if (!TryFindAgentSnapshot(agents, agentId, out SquadAgentSnapshot snap))
            {
                continue;
            }

            int score = snap.CanSuppress ? 2 : 0;
            score = checked(score + (snap.HasActiveThreat ? 1 : 0));
            if (score < 1)
            {
                continue;
            }

            if (score > bestScore || (score == bestScore && agentId.Value < bestAgentId))
            {
                bestLocal = i;
                bestScore = score;
                bestAgentId = agentId.Value;
            }
        }

        return bestLocal;
    }
}
