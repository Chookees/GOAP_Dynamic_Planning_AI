using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Goals;
using DynamicPlanningAI.Runtime.WorldState;
using Xunit;

namespace DynamicPlanningAI.UnitTests.Goals;

public sealed class GoalArbiterTests
{
    [Fact]
    public void SelectsHighestPriorityRelevantGoal()
    {
        GoalArbiter arbiter = new();
        Assert.Equal(OperationStatus.Success, arbiter.Register(new IdleGoal(GoalId.FromInt32(1))));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new EscapeGrenadeGoal(GoalId.FromInt32(2))));

        SymbolicWorldState world = new();
        world.Set(WorldFactId.GrenadeDangerPresent, 1);

        OperationStatus status = arbiter.Arbitrate(AgentId.FromInt32(1), world, 1L, out IGoapGoal? selected);
        Assert.Equal(OperationStatus.Success, status);
        Assert.NotNull(selected);
        Assert.Equal(GoalId.FromInt32(2), selected!.Id);
        Assert.True(arbiter.PlanRequested);
    }

    [Fact]
    public void HysteresisPreventsThrashing()
    {
        GoalArbiter arbiter = new();
        Assert.Equal(OperationStatus.Success, arbiter.Configure(hysteresisMargin: 100, cooldownTicks: 0));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new PatrolGoal(GoalId.FromInt32(1))));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new MaintainReadinessGoal(GoalId.FromInt32(2))));

        SymbolicWorldState world = new();
        world.Set(WorldFactId.ReadinessMaintained, 1);

        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 1L, out IGoapGoal? first));
        Assert.Equal(GoalId.FromInt32(2), first!.Id);

        // Patrol is lower priority and cannot displace MaintainReadiness under hysteresis.
        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 2L, out IGoapGoal? second));
        Assert.Equal(GoalId.FromInt32(2), second!.Id);
        Assert.False(arbiter.PlanRequested);
    }

    [Fact]
    public void CooldownBlocksReselection()
    {
        GoalArbiter arbiter = new();
        Assert.Equal(OperationStatus.Success, arbiter.Configure(hysteresisMargin: 0, cooldownTicks: 5));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new IdleGoal(GoalId.FromInt32(1))));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new EscapeGrenadeGoal(GoalId.FromInt32(2))));

        SymbolicWorldState world = new();
        world.Set(WorldFactId.GrenadeDangerPresent, 1);
        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 1L, out _));

        world.Clear(WorldFactId.GrenadeDangerPresent);
        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 2L, out IGoapGoal? idle));
        Assert.Equal(GoalId.FromInt32(1), idle!.Id);

        world.Set(WorldFactId.GrenadeDangerPresent, 1);
        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 3L, out IGoapGoal? blocked));
        Assert.Equal(GoalId.FromInt32(1), blocked!.Id);

        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 10L, out IGoapGoal? resumed));
        Assert.Equal(GoalId.FromInt32(2), resumed!.Id);
    }

    [Fact]
    public void NeverInterruptPolicyBlocksChallenge()
    {
        GoalArbiter arbiter = new();
        Assert.Equal(OperationStatus.Success, arbiter.Configure(0, 0));
        SurviveImmediateDangerGoal survive = new(GoalId.FromInt32(1));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new NeverYieldGoal(GoalId.FromInt32(10))));
        Assert.Equal(OperationStatus.Success, arbiter.Register(survive));

        SymbolicWorldState world = new();
        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 1L, out IGoapGoal? first));
        Assert.Equal(GoalId.FromInt32(10), first!.Id);

        world.Set(WorldFactId.ImmediateDangerPresent, 1);
        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 2L, out IGoapGoal? still));
        Assert.Equal(GoalId.FromInt32(10), still!.Id);
    }

    [Fact]
    public void TieBreakIsDeterministicByGoalId()
    {
        GoalArbiter arbiter = new();
        Assert.Equal(OperationStatus.Success, arbiter.Configure(0, 0));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new IdleGoal(GoalId.FromInt32(5))));
        Assert.Equal(OperationStatus.Success, arbiter.Register(new IdleGoal(GoalId.FromInt32(2))));

        SymbolicWorldState world = new();
        Assert.Equal(OperationStatus.Success, arbiter.Arbitrate(AgentId.FromInt32(1), world, 1L, out IGoapGoal? selected));
        Assert.Equal(GoalId.FromInt32(2), selected!.Id);
    }

    private sealed class NeverYieldGoal : GoapGoalBase
    {
        public NeverYieldGoal(GoalId id)
            : base(id, GoalInterruptionPolicy.Never, GoalPriorityBands.Idle)
        {
        }

        public override bool IsRelevant(in GoalArbitrationContext context) => true;

        public override OperationStatus BuildDesiredState(in GoalArbitrationContext context, SymbolicWorldState destination)
        {
            destination.Reset();
            return destination.Set(WorldFactId.ReadinessMaintained, 1);
        }
    }
}
