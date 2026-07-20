using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Memory;

namespace TacticalGoap.Runtime.Perception;

/// <summary>
/// Per-tick mutable context shared by perception sensors.
/// </summary>
/// <remarks>
/// Scratch buffers are allocated once by the owning runtime. Sensors must not
/// allocate on the frozen path and must not retain host spans.
/// </remarks>
public sealed class PerceptionContext
{
    /// <summary>
    /// Initializes a perception context with hard-limit scratch buffers.
    /// </summary>
    /// <param name="memory">Working-memory store written by sensors.</param>
    public PerceptionContext(WorkingMemoryStore memory)
    {
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        EntityScratch = new EntityId[AiHardLimits.MaximumPerceptionCandidates];
        DangerScratch = new Int2[AiHardLimits.MaximumDangerEvents];
        SoundScratch = new EntityId[AiHardLimits.MaximumSoundEvents];
        DamageScratch = new EntityId[AiHardLimits.MaximumDamageEvents];
        VisionRangeCells = 12;
        HearingRangeCells = 20;
        DangerInterruptRangeCells = 4;
        DefaultConfidence = 800;
        DefaultTtlTicks = 30L;
    }

    /// <summary>
    /// Gets the working-memory store.
    /// </summary>
    public WorkingMemoryStore Memory { get; }

    /// <summary>
    /// Gets the entity scratch buffer for perception candidates.
    /// </summary>
    public EntityId[] EntityScratch { get; }

    /// <summary>
    /// Gets the sound-event scratch buffer.
    /// </summary>
    public EntityId[] SoundScratch { get; }

    /// <summary>
    /// Gets the damage-source scratch buffer.
    /// </summary>
    public EntityId[] DamageScratch { get; }

    /// <summary>
    /// Gets the danger-position scratch buffer.
    /// </summary>
    public Int2[] DangerScratch { get; }

    /// <summary>
    /// Gets or sets the perceiving agent.
    /// </summary>
    public AgentId Agent { get; set; }

    /// <summary>
    /// Gets or sets the agent squad, when any.
    /// </summary>
    public SquadId Squad { get; set; }

    /// <summary>
    /// Gets or sets the agent cell position.
    /// </summary>
    public Int2 AgentPosition { get; set; }

    /// <summary>
    /// Gets or sets the agent facing.
    /// </summary>
    public Direction8 AgentFacing { get; set; }

    /// <summary>
    /// Gets or sets the current tick.
    /// </summary>
    public AiTick Tick { get; set; }

    /// <summary>
    /// Gets or sets vision range in cells.
    /// </summary>
    public int VisionRangeCells { get; set; }

    /// <summary>
    /// Gets or sets hearing range in cells (used only when position is known without omniscience).
    /// </summary>
    public int HearingRangeCells { get; set; }

    /// <summary>
    /// Gets or sets the distance at which danger raises an immediate interrupt.
    /// </summary>
    public int DangerInterruptRangeCells { get; set; }

    /// <summary>
    /// Gets or sets default confidence written for fresh evidence.
    /// </summary>
    public int DefaultConfidence { get; set; }

    /// <summary>
    /// Gets or sets default TTL in ticks for new records.
    /// </summary>
    public long DefaultTtlTicks { get; set; }

    /// <summary>
    /// Gets or sets whether danger requested an immediate interrupt this tick.
    /// </summary>
    public bool ImmediateInterruptRequested { get; set; }

    /// <summary>
    /// Gets or sets an optional line-of-sight service.
    /// </summary>
    public ILineOfSightService? LineOfSight { get; set; }

    /// <summary>
    /// Gets or sets an optional spatial query service.
    /// </summary>
    public ISpatialQueryService? Spatial { get; set; }

    /// <summary>
    /// Gets or sets an optional runtime trace sink.
    /// </summary>
    public IRuntimeTraceSink? Trace { get; set; }

    /// <summary>
    /// Resets per-tick interrupt state before sensors run.
    /// </summary>
    public void BeginTick(AgentId agent, SquadId squad, Int2 position, Direction8 facing, AiTick tick)
    {
        Agent = agent;
        Squad = squad;
        AgentPosition = position;
        AgentFacing = facing;
        Tick = tick;
        ImmediateInterruptRequested = false;
    }
}
