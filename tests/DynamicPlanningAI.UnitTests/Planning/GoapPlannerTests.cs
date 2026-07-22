using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;
using DynamicPlanningAI.Runtime.WorldState;
using Xunit;

namespace DynamicPlanningAI.UnitTests.Planning;

public sealed class GoapPlannerTests
{
    [Fact]
    public void EmptyGoal_ReturnsInvalidGoal()
    {
        PlanningSession session = CreateSession();
        SymbolicWorldState goal = new();
        SymbolicWorldState world = new();
        Assert.Equal(
            OperationStatus.Success,
            session.Begin(GoalId.FromInt32(1), 1L, goal, world, [], 0));

        GoapPlanner planner = new();
        PlannerResult result = planner.Step(session, 8);
        Assert.Equal(PlannerStatus.InvalidGoal, result.Status);
    }

    [Fact]
    public void AlreadySatisfied_ReturnsEmptySucceededPlan()
    {
        PlanningSession session = CreateSession();
        SymbolicWorldState goal = new();
        SymbolicWorldState world = new();
        goal.Set(WorldFactId.InCover, 1);
        world.Set(WorldFactId.InCover, 1);

        Assert.Equal(
            OperationStatus.Success,
            session.Begin(GoalId.FromInt32(1), 10L, goal, world, [], 0));

        GoapPlanner planner = new();
        PlannerResult result = planner.Step(session, 8);
        Assert.Equal(PlannerStatus.Succeeded, result.Status);
        Assert.Equal(0, result.PlanLength);
        Assert.True(session.OutputPlan.IsComplete);
    }

    [Fact]
    public void SingleAction_FindsPlan()
    {
        ActionCandidate takeCover = CreateCandidate(
            ActionId.FromInt32(1),
            cost: 5,
            preconditionFact: null,
            effectFact: WorldFactId.InCover,
            effectValue: 1);

        PlannerResult result = PlanTo(
            WorldFactId.InCover,
            worldSetup: null,
            [takeCover]);

        Assert.Equal(PlannerStatus.Succeeded, result.Status);
        Assert.Equal(1, result.PlanLength);
        Assert.Equal(5, result.TotalCost);
    }

    [Fact]
    public void MultiAction_FindsCheapestPlan()
    {
        ActionCandidate expensiveDirect = CreateCandidate(
            ActionId.FromInt32(1),
            cost: 50,
            preconditionFact: null,
            effectFact: WorldFactId.InCover,
            effectValue: 1);

        ActionCandidate move = CreateCandidate(
            ActionId.FromInt32(2),
            cost: 3,
            preconditionFact: null,
            effectFact: WorldFactId.AtTacticalPoint,
            effectValue: 1);

        ActionCandidate enterCover = CreateCandidate(
            ActionId.FromInt32(3),
            cost: 4,
            preconditionFact: WorldFactId.AtTacticalPoint,
            effectFact: WorldFactId.InCover,
            effectValue: 1);

        (PlannerResult result, Plan plan) = PlanToWithPlan(
            WorldFactId.InCover,
            worldSetup: null,
            [expensiveDirect, move, enterCover]);

        Assert.Equal(PlannerStatus.Succeeded, result.Status);
        Assert.Equal(2, result.PlanLength);
        Assert.Equal(7, result.TotalCost);
        Assert.True(plan.TryGet(0, out ActionCandidate first));
        Assert.Equal(ActionId.FromInt32(2), first.DefinitionId);
        Assert.True(plan.TryGet(1, out ActionCandidate second));
        Assert.Equal(ActionId.FromInt32(3), second.DefinitionId);
    }

    [Fact]
    public void NoPlan_WhenNoCandidateAdvancesGoal()
    {
        ActionCandidate irrelevant = CreateCandidate(
            ActionId.FromInt32(1),
            cost: 1,
            preconditionFact: null,
            effectFact: WorldFactId.WeaponLoaded,
            effectValue: 1);

        PlannerResult result = PlanTo(
            WorldFactId.InCover,
            worldSetup: null,
            [irrelevant]);

        Assert.Equal(PlannerStatus.NoPlan, result.Status);
    }

    [Fact]
    public void NodeCapacityExceeded_IsReported()
    {
        PlannerWorkspace workspace = new(nodeCapacity: 1, openCapacity: 1, closedCapacity: 8, candidateCapacity: 8, planCapacity: 4);
        PlanningSession session = new(workspace);
        SymbolicWorldState goal = new();
        SymbolicWorldState world = new();
        goal.Set(WorldFactId.InCover, 1);

        ActionCandidate takeCover = CreateCandidate(
            ActionId.FromInt32(1),
            cost: 1,
            preconditionFact: WorldFactId.CoverValid,
            effectFact: WorldFactId.InCover,
            effectValue: 1);

        Assert.Equal(
            OperationStatus.Success,
            session.Begin(GoalId.FromInt32(1), 1L, goal, world, [takeCover], 1));

        GoapPlanner planner = new();
        PlannerResult result = planner.Step(session, 8);
        Assert.Equal(PlannerStatus.NodeCapacityExceeded, result.Status);
    }

    [Fact]
    public void IncrementalSteps_ResumeUntilComplete()
    {
        ActionCandidate move = CreateCandidate(
            ActionId.FromInt32(1),
            cost: 2,
            preconditionFact: null,
            effectFact: WorldFactId.AtTacticalPoint,
            effectValue: 1);
        ActionCandidate cover = CreateCandidate(
            ActionId.FromInt32(2),
            cost: 2,
            preconditionFact: WorldFactId.AtTacticalPoint,
            effectFact: WorldFactId.InCover,
            effectValue: 1);

        PlannerWorkspace workspace = PlannerWorkspace.CreateDefault();
        PlanningSession session = new(workspace);
        SymbolicWorldState goal = new();
        SymbolicWorldState world = new();
        goal.Set(WorldFactId.InCover, 1);

        Assert.Equal(
            OperationStatus.Success,
            session.Begin(GoalId.FromInt32(1), 1L, goal, world, [move, cover], 2));

        GoapPlanner planner = new();
        PlannerResult first = planner.Step(session, 1);
        Assert.Equal(PlannerStatus.ExpansionBudgetExceeded, first.Status);

        PlannerResult second = planner.Step(session, 8);
        Assert.Equal(PlannerStatus.Succeeded, second.Status);
        Assert.Equal(2, second.PlanLength);
    }

    [Fact]
    public void WorkspaceReuse_AfterResetSucceedsAgain()
    {
        ActionCandidate takeCover = CreateCandidate(
            ActionId.FromInt32(1),
            cost: 1,
            preconditionFact: null,
            effectFact: WorldFactId.InCover,
            effectValue: 1);

        PlannerWorkspace workspace = PlannerWorkspace.CreateDefault();
        PlanningSession session = new(workspace);
        GoapPlanner planner = new();

        for (int i = 0; i < 2; i++)
        {
            SymbolicWorldState goal = new();
            SymbolicWorldState world = new();
            goal.Set(WorldFactId.InCover, 1);
            Assert.Equal(
                OperationStatus.Success,
                session.Begin(GoalId.FromInt32(i + 1), i, goal, world, [takeCover], 1));

            PlannerResult result = planner.Step(session, 8);
            Assert.Equal(PlannerStatus.Succeeded, result.Status);
            session.End();
        }
    }

    private static PlanningSession CreateSession()
    {
        return new PlanningSession(PlannerWorkspace.CreateDefault());
    }

    private static PlannerResult PlanTo(
        WorldFactId goalFact,
        WorldFactId? worldSetup,
        ActionCandidate[] candidates)
    {
        (PlannerResult result, _) = PlanToWithPlan(goalFact, worldSetup, candidates);
        return result;
    }

    private static (PlannerResult Result, Plan Plan) PlanToWithPlan(
        WorldFactId goalFact,
        WorldFactId? worldSetup,
        ActionCandidate[] candidates)
    {
        PlanningSession session = CreateSession();
        SymbolicWorldState goal = new();
        SymbolicWorldState world = new();
        goal.Set(goalFact, 1);
        if (worldSetup is WorldFactId setup)
        {
            world.Set(setup, 1);
        }

        Assert.Equal(
            OperationStatus.Success,
            session.Begin(GoalId.FromInt32(1), 1L, goal, world, candidates, candidates.Length));

        GoapPlanner planner = new();
        PlannerResult result = planner.Step(session, AiHardLimitsSafeExpansions());
        return (result, session.OutputPlan);
    }

    private static int AiHardLimitsSafeExpansions()
    {
        return 64;
    }

    private static ActionCandidate CreateCandidate(
        ActionId definitionId,
        int cost,
        WorldFactId? preconditionFact,
        WorldFactId effectFact,
        int effectValue)
    {
        SymbolicWorldState preconditions = new();
        SymbolicWorldState effects = new();
        if (preconditionFact is WorldFactId pre)
        {
            preconditions.Set(pre, 1);
        }

        effects.Set(effectFact, effectValue);

        return new ActionCandidate
        {
            DefinitionId = definitionId,
            CandidateId = ActionCandidateId.FromInt32(definitionId.Value),
            BoundEntityId = EntityId.Invalid,
            BoundPointId = TacticalPointId.Invalid,
            BoundNodeId = NavigationNodeId.Invalid,
            BoundWeaponId = WeaponId.Invalid,
            BoundOrderId = OrderId.Invalid,
            BoundSectorId = SearchSectorId.Invalid,
            Preconditions = preconditions,
            EffectSets = effects,
            EffectClearMask = 0UL,
            Cost = cost,
            ValidationStatus = OperationStatus.Success,
        };
    }
}
