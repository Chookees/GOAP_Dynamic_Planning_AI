using System;
using System.Collections.Generic;
using DynamicPlanningAI.Abstractions.Diagnostics;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Abstractions.Ticks;
using DynamicPlanningAI.Sample.World;

namespace DynamicPlanningAI.Sample.Hosting;

/// <summary>
/// Mutable simulation agent state for the console host.
/// </summary>
public sealed class SimAgentState
{
    /// <summary>
    /// Initializes agent state.
    /// </summary>
    public SimAgentState(AgentId id, EntityId entityId, Int2 position, bool isHostile)
    {
        Id = id;
        EntityId = entityId;
        Position = position;
        Facing = Direction8.East;
        Stance = AgentStance.Standing;
        IsAlive = true;
        IsHostile = isHostile;
        SelectedWeapon = WeaponId.FromInt32(1);
        Ammunition = 30;
        Grenades = 1;
        IsInCover = false;
        ActiveGoal = "Idle";
        ActiveAction = "None";
    }

    /// <summary>Gets the agent id.</summary>
    public AgentId Id { get; }

    /// <summary>Gets the entity id.</summary>
    public EntityId EntityId { get; }

    /// <summary>Gets or sets the cell position.</summary>
    public Int2 Position { get; set; }

    /// <summary>Gets or sets facing.</summary>
    public Direction8 Facing { get; set; }

    /// <summary>Gets or sets stance.</summary>
    public AgentStance Stance { get; set; }

    /// <summary>Gets or sets whether the agent is alive.</summary>
    public bool IsAlive { get; set; }

    /// <summary>Gets whether the agent is hostile.</summary>
    public bool IsHostile { get; }

    /// <summary>Gets or sets the selected weapon.</summary>
    public WeaponId SelectedWeapon { get; set; }

    /// <summary>Gets or sets remaining ammunition.</summary>
    public int Ammunition { get; set; }

    /// <summary>Gets or sets remaining grenades.</summary>
    public int Grenades { get; set; }

    /// <summary>Gets or sets whether the agent is in cover.</summary>
    public bool IsInCover { get; set; }

    /// <summary>Gets or sets the active goal name for tracing.</summary>
    public string ActiveGoal { get; set; }

    /// <summary>Gets or sets the active action name for tracing.</summary>
    public string ActiveAction { get; set; }
}

/// <summary>
/// Fixed-capacity in-memory trace sink used during ticks.
/// </summary>
public sealed class RingTraceSink : IRuntimeTraceSink
{
    private readonly TraceRecord[] _records;
    private int _count;
    private int _writeIndex;

    /// <summary>
    /// Initializes the ring buffer.
    /// </summary>
    /// <param name="capacity">Capacity ≤ hard limit.</param>
    public RingTraceSink(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumDiagnosticRecords)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _records = new TraceRecord[capacity];
        _count = 0;
        _writeIndex = 0;
    }

    /// <summary>Gets the number of records currently retained.</summary>
    public int Count => _count;

    /// <summary>Gets the buffer capacity.</summary>
    public int Capacity => _records.Length;

    /// <inheritdoc />
    public void Write(in TraceRecord record)
    {
        _records[_writeIndex] = record;
        _writeIndex = checked((_writeIndex + 1) % _records.Length);
        if (_count < _records.Length)
        {
            _count = checked(_count + 1);
        }
    }

    /// <summary>
    /// Copies records in chronological order into a caller list (off tick path).
    /// </summary>
    public void CopyTo(List<TraceRecord> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();
        int start = _count < _records.Length ? 0 : _writeIndex;
        for (int i = 0; i < _count && i < AiHardLimits.MaximumDiagnosticRecords; i++)
        {
            int index = checked((start + i) % _records.Length);
            destination.Add(_records[index]);
        }
    }

    /// <summary>
    /// Builds a deterministic digest string for replay comparison.
    /// </summary>
    public string BuildDigest()
    {
        System.Text.StringBuilder sb = new(_count * 24);
        int start = _count < _records.Length ? 0 : _writeIndex;
        for (int i = 0; i < _count && i < AiHardLimits.MaximumDiagnosticRecords; i++)
        {
            int index = checked((start + i) % _records.Length);
            TraceRecord r = _records[index];
            sb.Append(r.Tick.Sequence);
            sb.Append(':');
            sb.Append(r.Agent.Value);
            sb.Append(':');
            sb.Append(r.EventCode);
            sb.Append(':');
            sb.Append(r.PrimaryId);
            sb.Append(':');
            sb.Append(r.SecondaryId);
            sb.Append(':');
            sb.Append(r.ValueA);
            sb.Append(':');
            sb.Append(r.ValueB);
            sb.Append(':');
            sb.Append(r.StatusCode);
            sb.Append('|');
        }

        return sb.ToString();
    }
}

/// <summary>
/// Aggregated host adapters for the sample simulation.
/// </summary>
public sealed class SampleHostServices :
    IAgentBody,
    IAgentCommandSink,
    ILineOfSightService,
    ISpatialQueryService,
    IWeaponService,
    IPerceptionInput,
    ITacticalPointProvider,
    IDangerProvider,
    ICommunicationSink,
    IMovementService,
    IAnimationService,
    IInteractionService,
    ISmartObjectService
{
    private readonly SimWorld _world;
    private AgentId _bodyAgent;

    /// <summary>
    /// Initializes host services bound to a world.
    /// </summary>
    public SampleHostServices(SimWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _bodyAgent = AgentId.Invalid;
    }

    /// <summary>
    /// Selects which agent <see cref="IAgentBody"/> reads reflect.
    /// </summary>
    public void SetBodyAgent(AgentId agent) => _bodyAgent = agent;

    /// <inheritdoc />
    public Int2 Position => _world.RequireAgent(_bodyAgent).Position;

    /// <inheritdoc />
    public Direction8 Facing => _world.RequireAgent(_bodyAgent).Facing;

    /// <inheritdoc />
    public AgentStance Stance => _world.RequireAgent(_bodyAgent).Stance;

    /// <inheritdoc />
    public bool IsAlive => _world.RequireAgent(_bodyAgent).IsAlive;

    /// <inheritdoc />
    public bool IsIncapacitated => false;

    /// <inheritdoc />
    public OperationStatus SubmitMove(AgentId agent, Int2 destination)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null || !state.IsAlive)
        {
            return OperationStatus.NotFound;
        }

        if (!_world.Map.InBounds(destination) || !_world.Map.IsWalkable(destination, allowWindow: true))
        {
            return OperationStatus.HostRejected;
        }

        state.Position = destination;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus SubmitAim(AgentId agent, Direction8 facing)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            return OperationStatus.NotFound;
        }

        state.Facing = facing;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus SubmitFire(AgentId agent, WeaponId weapon, EntityId target)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            return OperationStatus.NotFound;
        }

        if (state.Ammunition < 1)
        {
            return OperationStatus.Failed;
        }

        state.Ammunition = checked(state.Ammunition - 1);
        _world.RecordFire(agent, target);
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus SubmitReload(AgentId agent, WeaponId weapon)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            return OperationStatus.NotFound;
        }

        state.Ammunition = 30;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus SubmitInteract(AgentId agent, SmartObjectId smartObject) =>
        BeginInteract(agent, smartObject);

    /// <inheritdoc />
    public OperationStatus SubmitAnimate(AgentId agent, int animationCode) => Play(agent, animationCode);

    /// <inheritdoc />
    public bool HasLineOfSight(Int2 from, Int2 destination) =>
        TryQueryLineOfSight(from, destination, out bool clear) == OperationStatus.Success && clear;

    /// <inheritdoc />
    public OperationStatus TryQueryLineOfSight(Int2 from, Int2 destination, out bool clear)
    {
        clear = false;
        if (!_world.Map.InBounds(from) || !_world.Map.InBounds(destination))
        {
            return OperationStatus.InvalidArgument;
        }

        clear = BresenhamClear(from, destination);
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus QueryEntitiesInRadius(Int2 origin, int radius, Span<EntityId> destination, out int writtenCount)
    {
        writtenCount = 0;
        if (radius < 0 || destination.Length < 1)
        {
            return OperationStatus.InvalidArgument;
        }

        for (int i = 0; i < _world.AgentCount && i < AiHardLimits.MaximumAgents; i++)
        {
            SimAgentState agent = _world.GetAgentAt(i);
            if (!agent.IsAlive)
            {
                continue;
            }

            if (Int2.ManhattanDistance(origin, agent.Position) > radius)
            {
                continue;
            }

            if (writtenCount >= destination.Length)
            {
                return OperationStatus.CapacityExceeded;
            }

            destination[writtenCount] = agent.EntityId;
            writtenCount = checked(writtenCount + 1);
        }

        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus TryGetEntityPosition(EntityId entity, out Int2 position)
    {
        if (_world.TryGetByEntity(entity, out SimAgentState? agent) && agent is not null)
        {
            position = agent.Position;
            return OperationStatus.Success;
        }

        position = Int2.Zero;
        return OperationStatus.NotFound;
    }

    /// <inheritdoc />
    public DistanceCategory ClassifyDistance(Int2 from, Int2 destination)
    {
        int d = Int2.ManhattanDistance(from, destination);
        if (d <= 2)
        {
            return DistanceCategory.Near;
        }

        if (d <= 6)
        {
            return DistanceCategory.Medium;
        }

        if (d <= 12)
        {
            return DistanceCategory.Far;
        }

        return DistanceCategory.OutOfRange;
    }

    /// <inheritdoc />
    public OperationStatus TryGetSelectedWeapon(AgentId agent, out WeaponId weapon)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            weapon = WeaponId.Invalid;
            return OperationStatus.NotFound;
        }

        weapon = state.SelectedWeapon;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus TryGetAmmunition(AgentId agent, WeaponId weapon, out int rounds)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            rounds = 0;
            return OperationStatus.NotFound;
        }

        rounds = state.Ammunition;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus CanFire(AgentId agent, WeaponId weapon, EntityId target)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null || state.Ammunition < 1)
        {
            return OperationStatus.Failed;
        }

        if (!_world.TryGetByEntity(target, out SimAgentState? targetState) || targetState is null || !targetState.IsAlive)
        {
            return OperationStatus.NotFound;
        }

        return HasLineOfSight(state.Position, targetState.Position)
            ? OperationStatus.Success
            : OperationStatus.Failed;
    }

    /// <inheritdoc />
    public OperationStatus TryGetRequiresReload(AgentId agent, WeaponId weapon, out bool requiresReload)
    {
        requiresReload = false;
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            return OperationStatus.NotFound;
        }

        requiresReload = state.Ammunition < 1;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus CopyVisibleEntities(AgentId agent, Span<EntityId> destination, out int writtenCount)
    {
        writtenCount = 0;
        if (!_world.TryGetAgent(agent, out SimAgentState? self) || self is null)
        {
            return OperationStatus.NotFound;
        }

        for (int i = 0; i < _world.AgentCount && i < AiHardLimits.MaximumAgents; i++)
        {
            SimAgentState other = _world.GetAgentAt(i);
            if (other.Id == agent || !other.IsAlive)
            {
                continue;
            }

            if (Int2.ManhattanDistance(self.Position, other.Position) > _world.VisionRange)
            {
                continue;
            }

            if (!HasLineOfSight(self.Position, other.Position))
            {
                continue;
            }

            if (writtenCount >= destination.Length)
            {
                return OperationStatus.CapacityExceeded;
            }

            destination[writtenCount] = other.EntityId;
            writtenCount = checked(writtenCount + 1);
        }

        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus CopyHeardEntities(AgentId agent, Span<EntityId> destination, out int writtenCount) =>
        CopyVisibleEntities(agent, destination, out writtenCount);

    /// <inheritdoc />
    public OperationStatus CopyDamageSources(AgentId agent, Span<EntityId> destination, out int writtenCount)
    {
        writtenCount = 0;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus CopyPoints(TacticalPointCategory category, Span<TacticalPointId> destination, out int writtenCount)
    {
        writtenCount = 0;
        for (int i = 0; i < _world.TacticalPointCount && i < AiHardLimits.MaximumTacticalPoints; i++)
        {
            if (_world.GetTacticalCategory(i) != category)
            {
                continue;
            }

            if (writtenCount >= destination.Length)
            {
                return OperationStatus.CapacityExceeded;
            }

            destination[writtenCount] = TacticalPointId.FromInt32(i);
            writtenCount = checked(writtenCount + 1);
        }

        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus TryGetPosition(TacticalPointId point, out Int2 position)
    {
        if (!point.IsValid || point.Value >= _world.TacticalPointCount)
        {
            position = Int2.Zero;
            return OperationStatus.NotFound;
        }

        position = _world.GetTacticalPosition(point.Value);
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus TryGetCategory(TacticalPointId point, out TacticalPointCategory category)
    {
        if (!point.IsValid || point.Value >= _world.TacticalPointCount)
        {
            category = TacticalPointCategory.Cover;
            return OperationStatus.NotFound;
        }

        category = _world.GetTacticalCategory(point.Value);
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public bool IsValid(TacticalPointId point) =>
        point.IsValid && point.Value < _world.TacticalPointCount;

    /// <inheritdoc />
    public OperationStatus CopyDangerPositions(Span<Int2> destination, out int writtenCount)
    {
        writtenCount = 0;
        for (int i = 0; i < _world.DangerCount && i < AiHardLimits.MaximumDangerEvents; i++)
        {
            if (writtenCount >= destination.Length)
            {
                return OperationStatus.CapacityExceeded;
            }

            destination[writtenCount] = _world.GetDangerAt(i);
            writtenCount = checked(writtenCount + 1);
        }

        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public bool IsPositionDangerous(Int2 position)
    {
        for (int i = 0; i < _world.DangerCount && i < AiHardLimits.MaximumDangerEvents; i++)
        {
            if (_world.GetDangerAt(i) == position)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public OperationStatus TryGetDangerIntensity(Int2 position, out int intensity)
    {
        if (IsPositionDangerous(position))
        {
            intensity = 100;
            return OperationStatus.Success;
        }

        intensity = 0;
        return OperationStatus.NotFound;
    }

    /// <inheritdoc />
    public OperationStatus Request(AgentId speaker, CommunicationIntentType intent, EntityId relatedEntity)
    {
        _world.RecordCommunication(speaker, (int)intent, relatedEntity);
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus Request(AgentId speaker, CommunicationIntentId intentId, EntityId relatedEntity)
    {
        _world.RecordCommunication(speaker, intentId.Value, relatedEntity);
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus TryGetPosition(AgentId agent, out Int2 position)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            position = Int2.Zero;
            return OperationStatus.NotFound;
        }

        position = state.Position;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus TryGetFacing(AgentId agent, out Direction8 facing)
    {
        if (!_world.TryGetAgent(agent, out SimAgentState? state) || state is null)
        {
            facing = Direction8.North;
            return OperationStatus.NotFound;
        }

        facing = state.Facing;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus TrySetDesiredDestination(AgentId agent, Int2 destination) =>
        SubmitMove(agent, destination);

    /// <inheritdoc />
    public OperationStatus TryStop(AgentId agent) => OperationStatus.Success;

    /// <inheritdoc />
    public OperationStatus Play(AgentId agent, int animationCode) => OperationStatus.Success;

    /// <inheritdoc />
    public OperationStatus TryIsPlaying(AgentId agent, int animationCode, out bool playing)
    {
        playing = false;
        return OperationStatus.Success;
    }

    /// <inheritdoc />
    public OperationStatus StopAnimation(AgentId agent) => OperationStatus.Success;

    /// <inheritdoc />
    public OperationStatus CanInteract(AgentId agent, SmartObjectId smartObject) =>
        smartObject.IsValid ? OperationStatus.Success : OperationStatus.InvalidArgument;

    /// <inheritdoc />
    public OperationStatus BeginInteract(AgentId agent, SmartObjectId smartObject)
    {
        if (!smartObject.IsValid)
        {
            return OperationStatus.InvalidArgument;
        }

        return _world.TryInteractSmartObject(agent, smartObject);
    }

    /// <inheritdoc />
    public OperationStatus EndInteract(AgentId agent, SmartObjectId smartObject) => OperationStatus.Success;

    /// <inheritdoc />
    public OperationStatus TryGetPosition(SmartObjectId smartObject, out Int2 position) =>
        _world.TryGetSmartObjectPosition(smartObject, out position);

    /// <inheritdoc />
    public bool IsAvailable(SmartObjectId smartObject) =>
        _world.IsSmartObjectAvailable(smartObject);

    /// <inheritdoc />
    public OperationStatus TryGetStateCode(SmartObjectId smartObject, out int stateCode) =>
        _world.TryGetSmartObjectState(smartObject, out stateCode);

    private bool BresenhamClear(Int2 from, Int2 to)
    {
        int x0 = from.X;
        int y0 = from.Y;
        int x1 = to.X;
        int y1 = to.Y;
        int dx = x1 >= x0 ? x1 - x0 : x0 - x1;
        int dy = y1 >= y0 ? y1 - y0 : y0 - y1;
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int x = x0;
        int y = y0;
        int maxSteps = checked(_world.Map.Width + _world.Map.Height + 2);
        for (int step = 0; step < maxSteps; step++)
        {
            Int2 cell = new(x, y);
            if (cell != from && cell != to)
            {
                CellKind kind = _world.Map.GetKind(cell);
                if (kind == CellKind.Wall || (kind == CellKind.Door && !_world.Map.IsDoorOpen(cell)))
                {
                    return false;
                }
            }

            if (x == x1 && y == y1)
            {
                return true;
            }

            int e2 = checked(2 * err);
            if (e2 > -dy)
            {
                err = checked(err - dy);
                x = checked(x + sx);
            }

            if (e2 < dx)
            {
                err = checked(err + dx);
                y = checked(y + sy);
            }
        }

        return false;
    }
}
