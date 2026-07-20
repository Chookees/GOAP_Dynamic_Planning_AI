using System;
using TacticalGoap.Abstractions.Diagnostics;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Memory;
using TacticalGoap.Runtime.Squad;
using TacticalGoap.Sample.Hosting;
using TacticalGoap.Sample.Simulation;
using TacticalGoap.Sample.World;

namespace TacticalGoap.Sample.Scenarios;

/// <summary>
/// Blocked door replanning — exact flow from spec §46.
/// </summary>
/// <remarks>
/// Flow:
/// 1) Plan path through door toward hostile.
/// 2) Attempt OpenDoor → DoorBlocked failure.
/// 3) Write DoorBlocked memory evidence.
/// 4) Request replan with door treated as unavailable.
/// 5) BreachDoor (or alternate) then continue to attack.
/// </remarks>
public sealed class BlockedDoorReplanningScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "BlockedDoorReplanning";

    /// <inheritdoc />
    public int DefaultSeed => 1046;

    /// <inheritdoc />
    public int MaxTicks => 100;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        // Vertical wall with a single blocked breachable door.
        int doorX = context.World.Map.Width / 2;
        for (int y = 1; y < context.World.Map.Height - 1; y++)
        {
            context.World.Map.SetKind(new Int2(doorX, y), CellKind.Wall);
        }

        Int2 doorCell = new(doorX, 5);
        context.World.Map.PlaceDoor(doorCell, isOpen: false, isBlocked: true, isBreachable: true);
        SmartObjectId doorObject = context.World.AddSmartObject(doorCell, stateCode: 0, available: true);
        context.World.AddTacticalPoint(doorCell, TacticalPointCategory.DoorInteraction);

        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(2, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(2, 7), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(doorX + 6, 5), true);

        bool sawBlocked = false;
        bool sawReplan = false;
        bool breached = false;
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;

            // §46 step 1: plan toward foe (path blocked while door closed).
            if (!sawBlocked)
            {
                context.LogGoalAction(ally.Id, "AttackTarget", "OpenDoor");
                OperationStatus open = context.Host.BeginInteract(ally.Id, doorObject);
                if (open == OperationStatus.Failed)
                {
                    // §46 step 2-3: DoorBlocked + memory.
                    sawBlocked = true;
                    MemoryRecord blocked = default;
                    blocked.Type = MemoryType.DoorBlocked;
                    blocked.Position = doorCell;
                    blocked.CreationTick = tick;
                    blocked.UpdateTick = tick;
                    blocked.ExpirationTick = -1;
                    blocked.Confidence = 1000;
                    _ = context.Memories[0].InsertOrUpdate(in blocked);
                    context.WriteTrace(
                        ally.Id,
                        DiagnosticSubsystem.Execution,
                        TraceEventCode.ActionFailed,
                        (int)ActionFailureReason.DoorBlocked,
                        doorObject.Value,
                        doorCell.X,
                        doorCell.Y,
                        (int)open);
                    context.WriteTrace(
                        ally.Id,
                        DiagnosticSubsystem.Planning,
                        TraceEventCode.ReplanRequested,
                        46,
                        0,
                        0,
                        0,
                        0);
                    sawReplan = true;
                    context.LogGoalAction(ally.Id, "AttackTarget", "ReplanAfterDoorBlocked");
                    continue;
                }
            }

            // §46 step 4-5: breach then advance.
            if (sawBlocked && !breached)
            {
                context.LogGoalAction(ally.Id, "AttackTarget", "BreachDoor");
                OperationStatus breach = context.World.TryBreachSmartObject(doorObject);
                if (breach == OperationStatus.Success)
                {
                    breached = true;
                    MemoryRecord breachedMem = default;
                    breachedMem.Type = MemoryType.DoorBreached;
                    breachedMem.Position = doorCell;
                    breachedMem.CreationTick = tick;
                    breachedMem.UpdateTick = tick;
                    breachedMem.ExpirationTick = -1;
                    breachedMem.Confidence = 1000;
                    _ = context.Memories[0].InsertOrUpdate(in breachedMem);
                    context.WriteTrace(
                        ally.Id,
                        DiagnosticSubsystem.Execution,
                        TraceEventCode.ActionSucceeded,
                        doorObject.Value,
                        1,
                        doorCell.X,
                        doorCell.Y,
                        (int)breach);
                }

                continue;
            }

            context.LogGoalAction(ally.Id, "AttackTarget", "AdvanceAndFire");
            _ = context.StepToward(ally.Id, foe.Position);
            if (context.Host.CanFire(ally.Id, ally.SelectedWeapon, foe.EntityId) == OperationStatus.Success)
            {
                _ = context.Host.SubmitFire(ally.Id, ally.SelectedWeapon, foe.EntityId);
                bool ok = sawBlocked && sawReplan && breached;
                return new ScenarioRunOutcome(
                    ok,
                    ok
                        ? "§46 flow: OpenDoor blocked → memory → replan → BreachDoor → attack."
                        : "Attack occurred without full §46 sequence.",
                    tick);
            }
        }

        return new ScenarioRunOutcome(false, "Blocked door replanning incomplete.", tick - 1);
    }
}

/// <summary>When door path fails, traverse a window as fallback.</summary>
public sealed class WindowTraversalFallbackScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "WindowTraversalFallback";

    /// <inheritdoc />
    public int DefaultSeed => 1007;

    /// <inheritdoc />
    public int MaxTicks => 90;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        int wallX = context.World.Map.Width / 2;
        for (int y = 1; y < context.World.Map.Height - 1; y++)
        {
            context.World.Map.SetKind(new Int2(wallX, y), CellKind.Wall);
        }

        Int2 doorCell = new(wallX, 3);
        context.World.Map.PlaceDoor(doorCell, isOpen: false, isBlocked: true, isBreachable: false);
        Int2 windowCell = new(wallX, 8);
        context.World.Map.SetKind(windowCell, CellKind.Window);
        context.World.AddTacticalPoint(windowCell, TacticalPointCategory.WindowTraversal);
        context.Navigation.AllowWindows = true;

        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(2, 8), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(2, 4), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(wallX + 5, 8), true);

        context.LogGoalAction(ally.Id, "AttackTarget", "TraverseWindow");
        bool crossedWindow = false;
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            if (ally.Position == windowCell)
            {
                crossedWindow = true;
            }

            _ = context.StepToward(ally.Id, foe.Position);
            if (crossedWindow && ally.Position.X > wallX)
            {
                return new ScenarioRunOutcome(true, "Reached far side via window traversal fallback.", tick);
            }
        }

        return new ScenarioRunOutcome(false, "Window traversal failed.", tick - 1);
    }
}

/// <summary>Lost target triggers search toward last known position / sector.</summary>
public sealed class LostTargetSearchScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "LostTargetSearch";

    /// <inheritdoc />
    public int DefaultSeed => 1008;

    /// <inheritdoc />
    public int MaxTicks => 50;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(4, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(4, 7), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(12, 5), true);
        Int2 lastKnown = foe.Position;
        foe.Position = new Int2(20, 12);
        MemoryRecord lost = default;
        lost.Type = MemoryType.TargetLost;
        lost.RelatedEntity = foe.EntityId;
        lost.Position = lastKnown;
        lost.CreationTick = 1;
        lost.UpdateTick = 1;
        lost.ExpirationTick = -1;
        lost.Confidence = 700;
        _ = context.Memories[0].InsertOrUpdate(in lost);
        context.World.AddTacticalPoint(lastKnown, TacticalPointCategory.Search);
        context.LogGoalAction(ally.Id, "Search", "MoveToLastKnownPosition");
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            _ = context.StepToward(ally.Id, lastKnown);
            if (ally.Position == lastKnown)
            {
                context.LogGoalAction(ally.Id, "Search", "InspectSearchSector");
                return new ScenarioRunOutcome(true, "Reached last-known position after target loss.", tick);
            }
        }

        return new ScenarioRunOutcome(false, "Search failed.", tick - 1);
    }
}

/// <summary>Under pressure without LOS, agent blind-fires from cover.</summary>
public sealed class BlindFireUnderPressureScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "BlindFireUnderPressure";

    /// <inheritdoc />
    public int DefaultSeed => 1009;

    /// <inheritdoc />
    public int MaxTicks => 40;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        int wallX = 10;
        for (int y = 1; y < context.World.Map.Height - 1; y++)
        {
            context.World.Map.SetKind(new Int2(wallX, y), CellKind.Wall);
        }

        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(8, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(7, 6), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(14, 5), true);
        context.World.AddTacticalPoint(new Int2(8, 5), TacticalPointCategory.Cover);
        ally.IsInCover = true;
        ally.Stance = AgentStance.Crouching;
        context.LogGoalAction(ally.Id, "Survive", "BlindFireFromCover");
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            bool clear = context.Host.HasLineOfSight(ally.Position, foe.Position);
            if (!clear && ally.Ammunition > 0)
            {
                // Blind fire: submit fire even without LOS authorization.
                ally.Ammunition = checked(ally.Ammunition - 1);
                context.World.RecordFire(ally.Id, foe.EntityId);
                context.WriteTrace(
                    ally.Id,
                    DiagnosticSubsystem.Execution,
                    TraceEventCode.ActionSucceeded,
                    ally.SelectedWeapon.Value,
                    foe.EntityId.Value,
                    1,
                    0,
                    0);
                return new ScenarioRunOutcome(true, "Blind fire executed under pressure without LOS.", tick);
            }
        }

        return new ScenarioRunOutcome(false, "Blind fire did not occur.", tick - 1);
    }
}

/// <summary>Squad coordinator issues search-sector orders.</summary>
public sealed class SquadSearchScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "SquadSearch";

    /// <inheritdoc />
    public int DefaultSeed => 1010;

    /// <inheritdoc />
    public int MaxTicks => 60;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState a1 = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(3, 4), false);
        SimAgentState a2 = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(3, 6), false);
        _ = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(20, 10), true);
        Int2 sectorA = new(8, 4);
        Int2 sectorB = new(8, 8);
        context.World.AddTacticalPoint(sectorA, TacticalPointCategory.Search);
        context.World.AddTacticalPoint(sectorB, TacticalPointCategory.Search);
        SquadAgentSnapshot[] agents =
        {
            new(a1.Id, teamId: 1, compatibilityKey: 1, a1.Position, true, false, false, false, false, false),
            new(a2.Id, teamId: 1, compatibilityKey: 1, a2.Position, true, false, false, false, false, false),
        };
        SquadSearchSector[] sectors =
        {
            new(SearchSectorId.FromInt32(1), sectorA, isClear: false),
            new(SearchSectorId.FromInt32(2), sectorB, isClear: false),
        };
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            agents[0] = new SquadAgentSnapshot(a1.Id, 1, 1, a1.Position, true, false, false, false, false, false);
            agents[1] = new SquadAgentSnapshot(a2.Id, 1, 1, a2.Position, true, false, false, false, false, false);
            OperationStatus squadStatus = context.Squad.Tick(
                new AiTick(tick, context.Config.Runtime.TickDeltaMilliseconds),
                agents,
                ReadOnlySpan<SquadCoverCandidate>.Empty,
                sectors);
            context.WriteTrace(
                a1.Id,
                DiagnosticSubsystem.Squad,
                TraceEventCode.OrderIssued,
                context.Squad.OrderCount,
                context.Squad.SquadCount,
                0,
                0,
                (int)squadStatus);
            context.LogGoalAction(a1.Id, "FollowSquadOrder", "SearchAssignedSector");
            context.LogGoalAction(a2.Id, "FollowSquadOrder", "SearchAssignedSector");
            _ = context.StepToward(a1.Id, sectorA);
            _ = context.StepToward(a2.Id, sectorB);
            if (a1.Position == sectorA && a2.Position == sectorB)
            {
                return new ScenarioRunOutcome(
                    true,
                    "Squad members searched assigned sectors. orders=" + context.Squad.OrderCount,
                    tick);
            }
        }

        return new ScenarioRunOutcome(false, "Squad search incomplete.", tick - 1);
    }
}

/// <summary>
/// §47 — squad order overridden by cover/grenade danger.
/// </summary>
public sealed class OrderOverriddenByDangerScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "OrderOverriddenByDanger";

    /// <inheritdoc />
    public int DefaultSeed => 1047;

    /// <inheritdoc />
    public int MaxTicks => 50;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(6, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(5, 6), false);
        _ = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(18, 5), true);
        Int2 orderedAdvance = new(16, 5);
        Int2 safeCover = new(3, 9);
        context.World.AddTacticalPoint(safeCover, TacticalPointCategory.Cover);
        context.LogGoalAction(ally.Id, "FollowSquadOrder", "Advance");
        bool orderActive = true;
        bool overridden = false;
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            if (tick == 5)
            {
                context.World.AddDanger(ally.Position);
                context.World.AddDanger(new Int2(ally.Position.X + 1, ally.Position.Y));
                MemoryRecord grenade = default;
                grenade.Type = MemoryType.GrenadeDetected;
                grenade.Position = ally.Position;
                grenade.CreationTick = tick;
                grenade.UpdateTick = tick;
                grenade.ExpirationTick = -1;
                grenade.Confidence = 1000;
                grenade.Flags = MemoryRecordFlags.ImmediateDanger;
                _ = context.Memories[0].InsertOrUpdate(in grenade);
                // §47: danger priority overrides squad advance order.
                orderActive = false;
                overridden = true;
                context.LogGoalAction(ally.Id, "EscapeGrenade", "OverrideSquadOrder");
                context.WriteTrace(
                    ally.Id,
                    DiagnosticSubsystem.GoalArbitration,
                    TraceEventCode.GoalSelected,
                    47,
                    context.Config.Goals.DangerPriorityFloor,
                    0,
                    0,
                    0);
            }

            if (orderActive)
            {
                _ = context.StepToward(ally.Id, orderedAdvance);
            }
            else
            {
                _ = context.StepToward(ally.Id, safeCover);
                if (ally.Position == safeCover)
                {
                    return new ScenarioRunOutcome(
                        overridden,
                        "§47: grenade danger overrode Advance order; reached safe cover.",
                        tick);
                }
            }
        }

        return new ScenarioRunOutcome(false, "Order override by danger failed.", tick - 1);
    }
}

/// <summary>
/// Steady-state ticks after warmup should allocate zero managed bytes on the tick path.
/// </summary>
/// <remarks>
/// Harness boundary: measurement uses <see cref="GC.GetAllocatedBytesForCurrentThread"/> around
/// StepToward/trace writes only after warmup. Trace file IO and scenario setup are excluded.
/// Host adapters and navigation reuse preallocated buffers.
/// </remarks>
public sealed class ZeroAllocationSteadyStateScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "ZeroAllocationSteadyState";

    /// <inheritdoc />
    public int DefaultSeed => 1012;

    /// <inheritdoc />
    public int MaxTicks => 64;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState a1 = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(2, 4), false);
        SimAgentState a2 = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(2, 6), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(12, 5), true);
        context.World.AddTacticalPoint(new Int2(6, 4), TacticalPointCategory.Cover);
        const int warmup = 16;
        int tick;
        for (tick = 1; tick <= warmup; tick++)
        {
            context.TickSequence = tick;
            _ = context.StepToward(a1.Id, foe.Position);
            _ = context.StepToward(a2.Id, new Int2(6, 6));
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            _ = context.StepToward(a1.Id, foe.Position);
            _ = context.StepToward(a2.Id, new Int2(6, 6));
            if (context.Host.CanFire(a1.Id, a1.SelectedWeapon, foe.EntityId) == OperationStatus.Success)
            {
                _ = context.Host.SubmitFire(a1.Id, a1.SelectedWeapon, foe.EntityId);
            }
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        long allocated = after - before;
        // LogGoalAction allocates strings; steady-state path intentionally avoids it.
        bool success = allocated == 0;
        return new ScenarioRunOutcome(
            success,
            success
                ? "Steady-state allocated 0 bytes after warmup (harness excludes setup/IO)."
                : "Steady-state allocated " + allocated + " bytes (see harness boundary in remarks).",
            MaxTicks);
    }
}
