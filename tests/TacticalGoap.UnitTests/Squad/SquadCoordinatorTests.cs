using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Squad;
using Xunit;

namespace TacticalGoap.UnitTests.Squad;

public sealed class SquadCoordinatorTests
{
    [Fact]
    public void Formation_RespectsMaxSquadSize()
    {
        SquadCoordinatorOptions options = new()
        {
            MaxAgentsPerSquad = 3,
            MaxSquads = 8,
            FormationProximity = 100,
            ReclusterIntervalTicks = 1,
            DefaultBehaviorDurationTicks = 100,
            DefaultOrderDurationTicks = 100,
        };

        SquadCoordinator coordinator = new(options);
        SquadAgentSnapshot[] agents = new SquadAgentSnapshot[7];
        for (int i = 0; i < agents.Length; i++)
        {
            agents[i] = CreateAgent(i, teamId: 1, compatibility: 1, new Int2(i, 0));
        }

        Assert.Equal(OperationStatus.Success, Tick(coordinator, 1, agents));
        Assert.Equal(3, coordinator.SquadCount);

        for (int s = 0; s < coordinator.SquadCount; s++)
        {
            Assert.True(coordinator.TryGetSquad(SquadId.FromInt32(s), out SquadRecord squad));
            Assert.True(squad.MemberCount <= 3);
            Assert.True(squad.MemberCount >= 1);
        }

        int totalMembers = 0;
        SquadMemberSlot[] buffer = new SquadMemberSlot[AiHardLimits.MaximumAgentsPerSquad];
        for (int s = 0; s < coordinator.SquadCount; s++)
        {
            Assert.Equal(OperationStatus.Success, coordinator.CopyMembers(SquadId.FromInt32(s), buffer, out int written));
            totalMembers += written;
        }

        Assert.Equal(7, totalMembers);
    }

    [Fact]
    public void SlotAssignment_IsDeterministic()
    {
        SquadCoordinator coordinator = CreateCoordinator();
        SquadAgentSnapshot[] agents =
        [
            CreateAgent(2, 1, 1, new Int2(0, 0), needsCover: true),
            CreateAgent(0, 1, 1, new Int2(1, 0), needsCover: true),
            CreateAgent(1, 1, 1, new Int2(2, 0), needsCover: true),
        ];

        SquadCoverCandidate[] cover =
        [
            new(TacticalPointId.FromInt32(10), new Int2(0, 1), capacity: 1),
            new(TacticalPointId.FromInt32(11), new Int2(1, 1), capacity: 1),
            new(TacticalPointId.FromInt32(12), new Int2(2, 1), capacity: 1),
        ];

        Assert.Equal(OperationStatus.Success, Tick(coordinator, 1, agents, cover));

        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out SquadOrder order0));
        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(1), out SquadOrder order1));
        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(2), out SquadOrder order2));

        Assert.Equal(SquadOrderType.MoveToCover, order0.OrderType);
        Assert.Equal(SquadOrderType.MoveToCover, order1.OrderType);
        Assert.Equal(SquadOrderType.MoveToCover, order2.OrderType);
        // Nearest cover by AgentId order: 0→11, 1→12, 2→10.
        Assert.Equal(11, order0.PointId.Value);
        Assert.Equal(12, order1.PointId.Value);
        Assert.Equal(10, order2.PointId.Value);

        // Re-run with shuffled snapshot order; assignments must match AgentId order.
        SquadCoordinator coordinator2 = CreateCoordinator();
        SquadAgentSnapshot[] shuffled =
        [
            agents[2],
            agents[0],
            agents[1],
        ];
        Assert.Equal(OperationStatus.Success, Tick(coordinator2, 1, shuffled, cover));
        Assert.True(coordinator2.TryGetActiveOrder(AgentId.FromInt32(0), out SquadOrder again0));
        Assert.Equal(order0.PointId, again0.PointId);
    }

    [Fact]
    public void Order_IssueAcknowledgeAndFail()
    {
        SquadCoordinator coordinator = CreateCoordinator();
        SquadAgentSnapshot[] agents =
        [
            CreateAgent(0, 1, 1, new Int2(0, 0), needsCover: true),
            CreateAgent(1, 1, 1, new Int2(1, 0), needsCover: true),
        ];
        SquadCoverCandidate[] cover =
        [
            new(TacticalPointId.FromInt32(5), new Int2(0, 1), 1),
            new(TacticalPointId.FromInt32(6), new Int2(1, 1), 1),
        ];

        Assert.Equal(OperationStatus.Success, Tick(coordinator, 1, agents, cover));
        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out SquadOrder order));
        Assert.Equal(SquadOrderStatus.Issued, order.Status);
        Assert.True(coordinator.TryGetFollowSquadOrderGoalData(AgentId.FromInt32(0), out FollowSquadOrderGoalData goal));
        Assert.Equal(order.OrderId, goal.OrderId);

        Assert.Equal(OperationStatus.Success, coordinator.AcknowledgeOrder(AgentId.FromInt32(0), order.OrderId));
        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out order));
        Assert.Equal(SquadOrderStatus.Acknowledged, order.Status);

        Assert.Equal(
            OperationStatus.Success,
            coordinator.ReportOrderFailed(AgentId.FromInt32(0), order.OrderId, ActionFailureReason.NoPath));
        Assert.False(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out _));
    }

    [Fact]
    public void SurvivalOverride_FailsOrderWhenAgentInDanger()
    {
        SquadCoordinator coordinator = CreateCoordinator();
        SquadAgentSnapshot[] agents =
        [
            CreateAgent(0, 1, 1, new Int2(0, 0), needsCover: true),
            CreateAgent(1, 1, 1, new Int2(1, 0), needsCover: true),
        ];
        SquadCoverCandidate[] cover =
        [
            new(TacticalPointId.FromInt32(1), new Int2(0, 1), 1),
            new(TacticalPointId.FromInt32(2), new Int2(1, 1), 1),
        ];

        Assert.Equal(OperationStatus.Success, Tick(coordinator, 1, agents, cover));
        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out SquadOrder order));

        agents[0] = CreateAgent(0, 1, 1, new Int2(0, 0), needsCover: true, isInDanger: true);
        Assert.Equal(OperationStatus.Success, Tick(coordinator, 2, agents, cover));
        Assert.False(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out _));
        Assert.True(coordinator.TryGetOrder(order.OrderId, out SquadOrder terminal));
        Assert.Equal(SquadOrderStatus.Failed, terminal.Status);
        Assert.True(terminal.SurvivalOverride);
    }

    [Fact]
    public void BehaviorTimeout_CleansUpOrders()
    {
        SquadCoordinatorOptions options = new()
        {
            MaxAgentsPerSquad = 8,
            FormationProximity = 50,
            ReclusterIntervalTicks = 1000,
            DefaultBehaviorDurationTicks = 2,
            DefaultOrderDurationTicks = 100,
        };
        SquadCoordinator coordinator = new(options);
        SquadAgentSnapshot[] agents =
        [
            CreateAgent(0, 1, 1, new Int2(0, 0), needsCover: true),
            CreateAgent(1, 1, 1, new Int2(1, 0), needsCover: true),
        ];
        SquadCoverCandidate[] cover =
        [
            new(TacticalPointId.FromInt32(1), new Int2(0, 1), 1),
            new(TacticalPointId.FromInt32(2), new Int2(1, 1), 1),
        ];

        Assert.Equal(OperationStatus.Success, Tick(coordinator, 1, agents, cover));
        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out SquadOrder firstOrder));
        Assert.True(coordinator.TryGetSquad(SquadId.FromInt32(0), out SquadRecord squad));
        Assert.Equal(SquadBehaviorType.GetToCover, squad.ActiveBehavior);

        // Behavior started at 1, duration 2 => expiry 3. Tick 4 ends behavior (sequence > expiry).
        Assert.Equal(OperationStatus.Success, Tick(coordinator, 4, agents, cover));
        Assert.True(coordinator.TryGetOrder(firstOrder.OrderId, out SquadOrder terminal));
        Assert.Equal(SquadOrderStatus.Cancelled, terminal.Status);
    }

    [Fact]
    public void RequestBehavior_SearchIssuesSectorOrders()
    {
        SquadCoordinator coordinator = CreateCoordinator();
        SquadAgentSnapshot[] agents =
        [
            CreateAgent(0, 1, 1, new Int2(0, 0)),
            CreateAgent(1, 1, 1, new Int2(1, 0)),
        ];
        SquadSearchSector[] sectors =
        [
            new(SearchSectorId.FromInt32(3), new Int2(5, 5), isClear: false),
            new(SearchSectorId.FromInt32(4), new Int2(6, 5), isClear: false),
        ];

        Assert.Equal(OperationStatus.Success, Tick(coordinator, 1, agents, sectors: sectors));
        Assert.Equal(OperationStatus.Success, coordinator.RequestBehavior(SquadId.FromInt32(0), SquadBehaviorType.Search));
        Assert.Equal(OperationStatus.Success, Tick(coordinator, 2, agents, sectors: sectors));

        Assert.True(coordinator.TryGetActiveOrder(AgentId.FromInt32(0), out SquadOrder order0));
        Assert.Equal(SquadOrderType.SearchSector, order0.OrderType);
        Assert.Equal(3, order0.SectorId.Value);
    }

    private static SquadCoordinator CreateCoordinator()
    {
        return new SquadCoordinator(new SquadCoordinatorOptions
        {
            MaxAgentsPerSquad = 8,
            FormationProximity = 50,
            ReclusterIntervalTicks = 1000,
            DefaultBehaviorDurationTicks = 60,
            DefaultOrderDurationTicks = 45,
        });
    }

    private static OperationStatus Tick(
        SquadCoordinator coordinator,
        long sequence,
        SquadAgentSnapshot[] agents,
        SquadCoverCandidate[]? cover = null,
        SquadSearchSector[]? sectors = null)
    {
        return coordinator.Tick(
            new AiTick(sequence, 16),
            agents,
            cover ?? [],
            sectors ?? []);
    }

    private static SquadAgentSnapshot CreateAgent(
        int id,
        int teamId,
        int compatibility,
        Int2 position,
        bool needsCover = false,
        bool isInDanger = false,
        bool hasActiveThreat = false,
        bool isSeparated = false,
        bool canSuppress = false)
    {
        return new SquadAgentSnapshot(
            AgentId.FromInt32(id),
            teamId,
            compatibility,
            position,
            isAlive: true,
            isInDanger,
            hasActiveThreat,
            needsCover,
            isSeparated,
            canSuppress);
    }
}
