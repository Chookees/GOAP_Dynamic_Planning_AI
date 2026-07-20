using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
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

/// <summary>Basic attack: allies advance and fire on a hostile.</summary>
public sealed class BasicAttackScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "BasicAttack";

    /// <inheritdoc />
    public int DefaultSeed => 1001;

    /// <inheritdoc />
    public int MaxTicks => 80;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState a1 = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(2, 4), false);
        SimAgentState a2 = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(2, 6), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(18, 5), true);
        context.World.AddTacticalPoint(new Int2(8, 4), TacticalPointCategory.Cover);
        context.World.AddTacticalPoint(new Int2(8, 6), TacticalPointCategory.Cover);
        int fires = 0;
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            context.LogGoalAction(a1.Id, "AttackTarget", "MoveOrFire");
            context.LogGoalAction(a2.Id, "AttackTarget", "MoveOrFire");
            _ = context.StepToward(a1.Id, foe.Position);
            _ = context.StepToward(a2.Id, foe.Position);
            if (context.Host.CanFire(a1.Id, a1.SelectedWeapon, foe.EntityId) == OperationStatus.Success)
            {
                _ = context.Host.SubmitFire(a1.Id, a1.SelectedWeapon, foe.EntityId);
                fires = checked(fires + 1);
            }

            if (context.Host.CanFire(a2.Id, a2.SelectedWeapon, foe.EntityId) == OperationStatus.Success)
            {
                _ = context.Host.SubmitFire(a2.Id, a2.SelectedWeapon, foe.EntityId);
                fires = checked(fires + 1);
            }

            if (fires >= 2)
            {
                foe.IsAlive = false;
                return new ScenarioRunOutcome(true, "Hostile engaged with successful fire.", tick);
            }
        }

        return new ScenarioRunOutcome(false, "Failed to complete attack within budget.", tick - 1);
    }
}

/// <summary>Take cover under fire using CoverEvaluator.</summary>
public sealed class TakeCoverScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "TakeCover";

    /// <inheritdoc />
    public int DefaultSeed => 1002;

    /// <inheritdoc />
    public int MaxTicks => 60;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(3, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(3, 7), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(16, 5), true);
        context.World.AddTacticalPoint(new Int2(7, 4), TacticalPointCategory.Cover);
        context.World.AddTacticalPoint(new Int2(7, 7), TacticalPointCategory.Cover);
        if (!context.TrySelectCover(ally.Id, foe.Position, out _, out Int2 coverPos))
        {
            return new ScenarioRunOutcome(false, "No cover selected.", 0);
        }

        context.LogGoalAction(ally.Id, "TakeCover", "MoveToCover");
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            if (ally.Position == coverPos)
            {
                ally.IsInCover = true;
                ally.Stance = AgentStance.Crouching;
                context.LogGoalAction(ally.Id, "TakeCover", "CrouchInCover");
                return new ScenarioRunOutcome(true, "Agent reached evaluated cover.", tick);
            }

            _ = context.StepToward(ally.Id, coverPos);
        }

        return new ScenarioRunOutcome(false, "Did not reach cover.", tick - 1);
    }
}

/// <summary>Advance while a teammate provides suppression.</summary>
public sealed class AdvanceUnderSuppressionScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "AdvanceUnderSuppression";

    /// <inheritdoc />
    public int DefaultSeed => 1003;

    /// <inheritdoc />
    public int MaxTicks => 70;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState suppressor = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(4, 3), false);
        SimAgentState advancer = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(4, 8), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(18, 5), true);
        context.World.AddTacticalPoint(new Int2(10, 8), TacticalPointCategory.Cover);
        Int2 advancePoint = new(10, 8);
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            context.LogGoalAction(suppressor.Id, "Suppress", "ProvideSuppression");
            context.LogGoalAction(advancer.Id, "AdvanceUnderSuppression", "AdvanceToCover");
            if (context.Host.CanFire(suppressor.Id, suppressor.SelectedWeapon, foe.EntityId) == OperationStatus.Success)
            {
                _ = context.Host.SubmitFire(suppressor.Id, suppressor.SelectedWeapon, foe.EntityId);
            }

            _ = context.StepToward(advancer.Id, advancePoint);
            if (advancer.Position == advancePoint)
            {
                return new ScenarioRunOutcome(true, "Advancer reached cover under suppression.", tick);
            }
        }

        return new ScenarioRunOutcome(false, "Advance failed.", tick - 1);
    }
}

/// <summary>
/// Emergent side attack via flank-weighted cover scoring (no explicit Flank action).
/// </summary>
/// <remarks>
/// Demonstrates REQ-COVER-001 emergent flanking: CoverEvaluator flank-angle bonus
/// selects a side cover without a dedicated Flank action in the catalog.
/// </remarks>
public sealed class EmergentSideAttackScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "EmergentSideAttack";

    /// <inheritdoc />
    public int DefaultSeed => 1004;

    /// <inheritdoc />
    public int MaxTicks => 80;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(3, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(3, 7), false);
        SimAgentState foe = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(14, 5), true);
        foe.Facing = Direction8.West;
        // Frontal cover vs side cover — evaluator prefers flank angle.
        context.World.AddTacticalPoint(new Int2(10, 5), TacticalPointCategory.Cover);
        context.World.AddTacticalPoint(new Int2(14, 2), TacticalPointCategory.Cover);
        context.World.AddTacticalPoint(new Int2(14, 8), TacticalPointCategory.Cover);
        // Invalidate frontal approach so flank-angle side cover wins.
        context.World.AddDanger(new Int2(10, 5));
        if (!context.TrySelectCover(ally.Id, foe.Position, out TacticalPointId selected, out Int2 coverPos))
        {
            return new ScenarioRunOutcome(false, "Cover selection failed.", 0);
        }

        bool isSide = coverPos.Y != foe.Position.Y;
        context.LogGoalAction(ally.Id, "AttackFromCover", "MoveToSideCover");
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            _ = context.StepToward(ally.Id, coverPos);
            if (ally.Position == coverPos)
            {
                ally.IsInCover = true;
                if (context.Host.CanFire(ally.Id, ally.SelectedWeapon, foe.EntityId) == OperationStatus.Success)
                {
                    _ = context.Host.SubmitFire(ally.Id, ally.SelectedWeapon, foe.EntityId);
                }

                string summary = isSide
                    ? "Selected side cover via flank scoring (no Flank action). point=" + selected.Value
                    : "Selected cover point=" + selected.Value;
                return new ScenarioRunOutcome(isSide, summary, tick);
            }
        }

        return new ScenarioRunOutcome(false, "Failed to reach side cover.", tick - 1);
    }
}

/// <summary>Grenade danger forces escape away from the blast cell.</summary>
public sealed class GrenadeEscapeScenario : IScenario
{
    /// <inheritdoc />
    public string Name => "GrenadeEscape";

    /// <inheritdoc />
    public int DefaultSeed => 1005;

    /// <inheritdoc />
    public int MaxTicks => 40;

    /// <inheritdoc />
    public ScenarioRunOutcome Run(SimulationContext context)
    {
        SimAgentState ally = context.World.AddAgent(AgentId.FromInt32(1), EntityId.FromInt32(1), new Int2(8, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(2), EntityId.FromInt32(2), new Int2(4, 5), false);
        _ = context.World.AddAgent(AgentId.FromInt32(3), EntityId.FromInt32(3), new Int2(18, 5), true);
        Int2 blast = new(8, 5);
        context.World.AddDanger(blast);
        context.World.AddDanger(new Int2(8, 4));
        context.World.AddDanger(new Int2(8, 6));
        context.World.AddTacticalPoint(new Int2(3, 8), TacticalPointCategory.Fallback);
        MemoryRecord grenade = default;
        grenade.Type = MemoryType.GrenadeDetected;
        grenade.Position = blast;
        grenade.CreationTick = 1;
        grenade.UpdateTick = 1;
        grenade.ExpirationTick = -1;
        grenade.Confidence = 1000;
        grenade.Flags = MemoryRecordFlags.ImmediateDanger;
        _ = context.Memories[0].InsertOrUpdate(in grenade);
        Int2 escape = new(3, 8);
        context.LogGoalAction(ally.Id, "EscapeGrenade", "FleeDanger");
        int tick;
        for (tick = 1; tick <= MaxTicks; tick++)
        {
            context.TickSequence = tick;
            _ = context.StepToward(ally.Id, escape);
            if (!context.Host.IsPositionDangerous(ally.Position) && ally.Position == escape)
            {
                return new ScenarioRunOutcome(true, "Escaped grenade danger zone.", tick);
            }
        }

        return new ScenarioRunOutcome(false, "Failed to escape grenade.", tick - 1);
    }
}
