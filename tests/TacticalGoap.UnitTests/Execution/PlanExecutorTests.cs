using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Actions;
using TacticalGoap.Runtime.Execution;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Runtime.WorldState;
using Xunit;

namespace TacticalGoap.UnitTests.Execution;

public sealed class PlanExecutorTests
{
    [Fact]
    public void FailureInvalidatesPlanAndRequestsReplan()
    {
        ActionDefinitionRegistry registry = new();
        FailOnceAction failAction = new();
        Assert.Equal(
            OperationStatus.Success,
            registry.Register(FailOnceAction.CreateDefinition(), failAction));

        ReplanPolicy policy = new(maxAttempts: 3, minDelayTicks: 0, allowEmergencyInterrupt: true);
        FailureKnowledgeWriter knowledge = new();
        PlanExecutor executor = new(registry, policy, knowledge);

        Plan plan = new(4);
        ActionCandidate candidate = ActionFactory.CreateCandidate(
            FailOnceAction.CreateDefinition(),
            EntityId.Invalid,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        candidate.CandidateId = ActionCandidateId.FromInt32(0);
        ActionCandidate[] buffer = [candidate];
        Assert.Equal(
            OperationStatus.Success,
            plan.Assign(GoalId.FromInt32(1), 1L, 1UL, buffer, 1, 1));

        Assert.Equal(OperationStatus.Success, executor.Install(plan));

        SymbolicWorldState world = new();
        ActionTickResult result = executor.Tick(
            AgentId.FromInt32(1),
            world,
            tickSequence: 5L,
            deltaMilliseconds: 16,
            hostServices: default);

        Assert.True(result.IsFailed);
        Assert.True(executor.ReplanRequested);
        Assert.NotNull(executor.ActivePlan);
        Assert.NotEqual(PlannerStatus.Succeeded, executor.ActivePlan!.Status);
    }

    private sealed class FailOnceAction : ActionExecutorBase
    {
        public FailOnceAction()
            : base(ActionId.FromInt32(900), 1)
        {
        }

        public static ActionDefinition CreateDefinition()
        {
            return ActionFactory.CreateDefinition(
                ActionId.FromInt32(900),
                1,
                null,
                0,
                WorldFactId.ReadinessMaintained,
                1,
                null);
        }

        protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
        {
            return ActionFailureReason.HostServiceFailure;
        }
    }
}
