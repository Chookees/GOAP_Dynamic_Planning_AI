using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Actions;
using DynamicPlanningAI.Runtime.Execution;
using DynamicPlanningAI.Runtime.Planning;
using DynamicPlanningAI.Runtime.WorldState;
using Xunit;

namespace DynamicPlanningAI.UnitTests.Actions;

public sealed class RepresentativeActionTests
{
    [Fact]
    public void OpenDoor_FailureWritesDoorBlockedKnowledge()
    {
        OpenDoorAction action = new();
        FailureKnowledgeWriter knowledge = new();
        SymbolicWorldState world = CreateWorld(WorldFactId.DoorKnown, 1);
        ActionCandidate candidate = BindEntity(OpenDoorAction.CreateDefinition(), EntityId.FromInt32(7));

        ActionExecutionContext context = CreateContext(world, candidate, knowledge);
        context.Interaction = new RejectInteractionService();

        ActionTickResult begin = action.Begin(ref context);
        Assert.Equal(ActionStatus.Failed, begin.Status);
        Assert.Equal(ActionFailureReason.DoorBlocked, begin.FailureReason);
        Assert.Equal(1, knowledge.Count);
        Assert.True(knowledge.TryGet(0, out _, out MemoryType type, out int primary, out _, out _));
        Assert.Equal(MemoryType.DoorBlocked, type);
        Assert.Equal(7, primary);
    }

    [Fact]
    public void AttackTarget_SucceedsWithHostFire()
    {
        AttackTargetAction action = new();
        SymbolicWorldState world = CreateWorld(WorldFactId.TargetVisible, 1);
        world.Set(WorldFactId.TargetAlive, 1);
        ActionCandidate candidate = BindEntityWeapon(AttackTargetAction.CreateDefinition(), EntityId.FromInt32(3), WeaponId.FromInt32(1));
        ActionExecutionContext context = CreateContext(world, candidate, null);
        context.Commands = new AcceptCommandSink();
        context.Weapons = new AcceptWeaponService();
        context.RequiredProgressTicks = 1;

        Assert.False(action.Begin(ref context).IsFailed);
        ActionTickResult tick = action.Tick(ref context);
        Assert.True(tick.IsSucceeded);
        Assert.True(world.TryGet(WorldFactId.TargetAlive, out int alive));
        Assert.Equal(0, alive);
    }

    [Fact]
    public void ReloadWeapon_SetsWeaponLoaded()
    {
        ReloadWeaponAction action = new();
        SymbolicWorldState world = CreateWorld(WorldFactId.HasAmmunition, 1);
        ActionCandidate candidate = BindWeapon(ReloadWeaponAction.CreateDefinition(), WeaponId.FromInt32(1));
        ActionExecutionContext context = CreateContext(world, candidate, null);
        context.Commands = new AcceptCommandSink();
        context.Weapons = new AcceptWeaponService();
        context.RequiredProgressTicks = 1;

        Assert.False(action.Begin(ref context).IsFailed);
        Assert.True(action.Tick(ref context).IsSucceeded);
        Assert.True(world.TryGet(WorldFactId.WeaponLoaded, out int loaded) && loaded == 1);
    }

    [Fact]
    public void MoveToCover_ProgressesWithCommands()
    {
        MoveToCoverAction action = new();
        SymbolicWorldState world = CreateWorld(WorldFactId.MovementDestinationSet, 1);
        ActionCandidate candidate = ActionFactory.CreateCandidate(
            MoveToCoverAction.CreateDefinition(),
            EntityId.Invalid,
            TacticalPointId.FromInt32(4),
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        ActionExecutionContext context = CreateContext(world, candidate, null);
        context.Commands = new AcceptCommandSink();
        context.Movement = new AcceptMovementService();
        context.RequiredProgressTicks = 1;

        Assert.False(action.Begin(ref context).IsFailed);
        Assert.True(action.Tick(ref context).IsSucceeded);
        Assert.True(world.TryGet(WorldFactId.InCover, out int cover) && cover == 1);
    }

    [Fact]
    public void TraverseWindow_CompletesTraversal()
    {
        TraverseWindowAction action = new();
        SymbolicWorldState world = CreateWorld(WorldFactId.AtWindowTraversal, 1);
        ActionCandidate candidate = ActionFactory.CreateCandidate(
            TraverseWindowAction.CreateDefinition(),
            EntityId.Invalid,
            TacticalPointId.FromInt32(1),
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        ActionExecutionContext context = CreateContext(world, candidate, null);
        context.Animation = new AcceptAnimationService();
        context.RequiredProgressTicks = 1;

        Assert.False(action.Begin(ref context).IsFailed);
        Assert.True(action.Tick(ref context).IsSucceeded);
        Assert.True(world.TryGet(WorldFactId.WindowTraversed, out int traversed) && traversed == 1);
    }

    [Fact]
    public void EscapeGrenade_ResolvesDanger()
    {
        EscapeGrenadeAction action = new();
        SymbolicWorldState world = CreateWorld(WorldFactId.GrenadeDangerPresent, 1);
        ActionCandidate candidate = ActionFactory.CreateCandidate(
            EscapeGrenadeAction.CreateDefinition(),
            EntityId.Invalid,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        ActionExecutionContext context = CreateContext(world, candidate, null);
        context.Commands = new AcceptCommandSink();
        context.Animation = new AcceptAnimationService();
        context.RequiredProgressTicks = 1;

        Assert.False(action.Begin(ref context).IsFailed);
        Assert.True(action.Tick(ref context).IsSucceeded);
        Assert.True(world.TryGet(WorldFactId.ImmediateDangerResolved, out int resolved) && resolved == 1);
        Assert.False(world.TryGet(WorldFactId.GrenadeDangerPresent, out _));
    }

    [Fact]
    public void Idle_MaintainsReadiness()
    {
        IdleAction action = new();
        SymbolicWorldState world = new();
        ActionCandidate candidate = ActionFactory.CreateCandidate(
            IdleAction.CreateDefinition(),
            EntityId.Invalid,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        ActionExecutionContext context = CreateContext(world, candidate, null);
        context.RequiredProgressTicks = 1;

        Assert.False(action.Begin(ref context).IsFailed);
        Assert.True(action.Tick(ref context).IsSucceeded);
        Assert.True(world.TryGet(WorldFactId.ReadinessMaintained, out int ready) && ready == 1);
    }

    [Fact]
    public void AcceptSquadOrder_RequiresOrderBinding()
    {
        AcceptSquadOrderAction action = new();
        BoundedCandidateWriter writer = new();
        CandidateGenerationContext generation = new(
            AgentId.FromInt32(1),
            new SymbolicWorldState(),
            EntityId.Invalid,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.FromInt32(9),
            SearchSectorId.Invalid,
            null,
            null);

        int written = action.Generate(in generation, AcceptSquadOrderAction.CreateDefinition(), writer);
        Assert.Equal(1, written);

        SymbolicWorldState world = CreateWorld(WorldFactId.SquadOrderAvailable, 1);
        ActionExecutionContext context = CreateContext(world, writer.Buffer[0], null);
        context.RequiredProgressTicks = 1;
        Assert.False(action.Begin(ref context).IsFailed);
        Assert.True(action.Tick(ref context).IsSucceeded);
    }

    private static SymbolicWorldState CreateWorld(WorldFactId fact, int value)
    {
        SymbolicWorldState world = new();
        world.Set(fact, value);
        return world;
    }

    private static ActionCandidate BindEntity(ActionDefinition definition, EntityId entity)
    {
        return ActionFactory.CreateCandidate(
            definition,
            entity,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
    }

    private static ActionCandidate BindEntityWeapon(ActionDefinition definition, EntityId entity, WeaponId weapon)
    {
        return ActionFactory.CreateCandidate(
            definition,
            entity,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            weapon,
            OrderId.Invalid,
            SearchSectorId.Invalid);
    }

    private static ActionCandidate BindWeapon(ActionDefinition definition, WeaponId weapon)
    {
        return ActionFactory.CreateCandidate(
            definition,
            EntityId.Invalid,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            weapon,
            OrderId.Invalid,
            SearchSectorId.Invalid);
    }

    private static ActionExecutionContext CreateContext(
        SymbolicWorldState world,
        ActionCandidate candidate,
        FailureKnowledgeWriter? knowledge)
    {
        return new ActionExecutionContext
        {
            AgentId = AgentId.FromInt32(1),
            Candidate = candidate,
            WorldState = world,
            TickSequence = 1L,
            DeltaMilliseconds = 16,
            MaximumTicks = 32,
            FailureKnowledge = knowledge,
            RequiredProgressTicks = 2,
        };
    }

    private sealed class RejectInteractionService : IInteractionService
    {
        public OperationStatus CanInteract(AgentId agent, SmartObjectId smartObject) => OperationStatus.HostRejected;
        public OperationStatus BeginInteract(AgentId agent, SmartObjectId smartObject) => OperationStatus.HostRejected;
        public OperationStatus EndInteract(AgentId agent, SmartObjectId smartObject) => OperationStatus.Success;
    }

    private sealed class AcceptCommandSink : IAgentCommandSink
    {
        public OperationStatus SubmitMove(AgentId agent, Int2 destination) => OperationStatus.Success;
        public OperationStatus SubmitAim(AgentId agent, Direction8 facing) => OperationStatus.Success;
        public OperationStatus SubmitFire(AgentId agent, WeaponId weapon, EntityId target) => OperationStatus.Success;
        public OperationStatus SubmitReload(AgentId agent, WeaponId weapon) => OperationStatus.Success;
        public OperationStatus SubmitInteract(AgentId agent, SmartObjectId smartObject) => OperationStatus.Success;
        public OperationStatus SubmitAnimate(AgentId agent, int animationCode) => OperationStatus.Success;
    }

    private sealed class AcceptWeaponService : IWeaponService
    {
        public OperationStatus TryGetSelectedWeapon(AgentId agent, out WeaponId weapon)
        {
            weapon = WeaponId.FromInt32(1);
            return OperationStatus.Success;
        }

        public OperationStatus TryGetAmmunition(AgentId agent, WeaponId weapon, out int rounds)
        {
            rounds = 10;
            return OperationStatus.Success;
        }

        public OperationStatus CanFire(AgentId agent, WeaponId weapon, EntityId target) => OperationStatus.Success;

        public OperationStatus TryGetRequiresReload(AgentId agent, WeaponId weapon, out bool requiresReload)
        {
            requiresReload = false;
            return OperationStatus.Success;
        }
    }

    private sealed class AcceptMovementService : IMovementService
    {
        public OperationStatus TryGetPosition(AgentId agent, out Int2 position)
        {
            position = default;
            return OperationStatus.Success;
        }

        public OperationStatus TryGetFacing(AgentId agent, out Direction8 facing)
        {
            facing = Direction8.North;
            return OperationStatus.Success;
        }

        public OperationStatus TrySetDesiredDestination(AgentId agent, Int2 destination) => OperationStatus.Success;
        public OperationStatus TryStop(AgentId agent) => OperationStatus.Success;
        public DistanceCategory ClassifyDistance(Int2 from, Int2 destination)
            => DistanceCategory.Near;
    }

    private sealed class AcceptAnimationService : IAnimationService
    {
        public OperationStatus Play(AgentId agent, int animationCode) => OperationStatus.Success;
        public OperationStatus TryIsPlaying(AgentId agent, int animationCode, out bool playing)
        {
            playing = false;
            return OperationStatus.Success;
        }

        public OperationStatus StopAnimation(AgentId agent) => OperationStatus.Success;
    }
}
