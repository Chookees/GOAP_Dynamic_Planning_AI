using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Lifecycle;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime;
using TacticalGoap.Runtime.Goals;
using Xunit;

namespace TacticalGoap.UnitTests.Runtime;

public sealed class AiRuntimeLifecycleTests
{
    [Fact]
    public void LifecycleTransitions_AndFreezeThenTick()
    {
        using AiRuntime runtime = new(agentCapacity: 4);
        Assert.Equal(RuntimeLifecycleState.Created, runtime.State);

        Assert.Equal(OperationStatus.Success, runtime.BeginConfiguration());
        Assert.Equal(RuntimeLifecycleState.Configuring, runtime.State);

        AgentId agent = AgentId.FromInt32(1);
        Assert.Equal(OperationStatus.Success, runtime.RegisterAgent(agent));
        Assert.Equal(OperationStatus.Success, runtime.RegisterGoalSet(agent, new IdleGoal(GoalId.FromInt32(1))));
        Assert.Equal(OperationStatus.Success, runtime.RegisterActionSet());
        Assert.Equal(OperationStatus.Success, runtime.RegisterTacticalData(0));

        Assert.Equal(OperationStatus.Success, runtime.Initialize());
        Assert.Equal(RuntimeLifecycleState.Initialized, runtime.State);
        Assert.Equal(OperationStatus.Success, runtime.Validate());
        Assert.Equal(OperationStatus.Success, runtime.Freeze());
        Assert.Equal(RuntimeLifecycleState.Frozen, runtime.State);

        Assert.Equal(OperationStatus.InvalidLifecycleState, runtime.RegisterAgent(AgentId.FromInt32(2)));

        OperationStatus tickStatus = runtime.Tick(new AiTick(1L, 16));
        Assert.Equal(OperationStatus.Success, tickStatus);
        Assert.Equal(RuntimeLifecycleState.Running, runtime.State);

        Assert.Equal(OperationStatus.Success, runtime.Stop());
        Assert.Equal(RuntimeLifecycleState.Stopped, runtime.State);
        Assert.Equal(OperationStatus.Success, runtime.ResetForNewScenario());
        Assert.Equal(RuntimeLifecycleState.Frozen, runtime.State);

        runtime.Dispose();
        Assert.Equal(RuntimeLifecycleState.Disposed, runtime.State);
        Assert.Equal(OperationStatus.InvalidLifecycleState, runtime.Tick(new AiTick(2L, 16)));
    }

    [Fact]
    public void RejectsNonMonotonicTickSequence()
    {
        using AiRuntime runtime = CreateFrozenRuntime();
        Assert.Equal(OperationStatus.Success, runtime.Tick(new AiTick(5L, 16)));
        Assert.Equal(OperationStatus.InvalidArgument, runtime.Tick(new AiTick(4L, 16)));
    }

    private static AiRuntime CreateFrozenRuntime()
    {
        AiRuntime runtime = new(2);
        Assert.Equal(OperationStatus.Success, runtime.BeginConfiguration());
        AgentId agent = AgentId.FromInt32(1);
        Assert.Equal(OperationStatus.Success, runtime.RegisterAgent(agent));
        Assert.Equal(OperationStatus.Success, runtime.RegisterGoalSet(agent, new IdleGoal(GoalId.FromInt32(1))));
        Assert.Equal(OperationStatus.Success, runtime.RegisterActionSet());
        Assert.Equal(OperationStatus.Success, runtime.RegisterTacticalData(0));
        Assert.Equal(OperationStatus.Success, runtime.Initialize());
        Assert.Equal(OperationStatus.Success, runtime.Freeze());
        return runtime;
    }
}
