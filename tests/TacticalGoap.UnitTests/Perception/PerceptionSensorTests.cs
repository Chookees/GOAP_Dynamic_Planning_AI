using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Memory;
using TacticalGoap.Runtime.Perception;
using Xunit;

namespace TacticalGoap.UnitTests.Perception;

public sealed class PerceptionSensorTests
{
    [Fact]
    public void VisionSensor_WritesSeenMemory_WithoutRevealingUnseenEntities()
    {
        WorkingMemoryStore memory = new(8);
        PerceptionContext context = CreateContext(memory);
        FakePerceptionInput input = new FakePerceptionInput();
        input.SetVisible(EntityId.FromInt32(7), EntityId.FromInt32(99));
        FakeSpatial spatial = new FakeSpatial();
        spatial.Set(EntityId.FromInt32(7), new Int2(2, 0));
        // Entity 99 has no known position -> must not be written from omniscience.
        context.Spatial = spatial;
        context.LineOfSight = new AlwaysClearLos();

        VisionSensor sensor = new VisionSensor();
        Assert.Equal(OperationStatus.Success, sensor.Tick(input, context));

        MemoryRecord[] buffer = new MemoryRecord[8];
        memory.CopyTo(buffer, out int count);
        Assert.Equal(1, count);
        Assert.Equal(MemoryType.TargetSeen, buffer[0].Type);
        Assert.Equal(EntityId.FromInt32(7), buffer[0].RelatedEntity);
        Assert.Equal(new Int2(2, 0), buffer[0].Position);
        Assert.Equal(MemoryRecordFlags.Visible, buffer[0].Flags);
    }

    [Fact]
    public void HearingSensor_WritesUncertainMemory_WithoutQueryingPositions()
    {
        WorkingMemoryStore memory = new(8);
        PerceptionContext context = CreateContext(memory);
        FakePerceptionInput input = new FakePerceptionInput();
        input.SetHeard(EntityId.FromInt32(4));
        TrackingSpatial spatial = new TrackingSpatial();
        context.Spatial = spatial;

        HearingSensor sensor = new HearingSensor();
        Assert.Equal(OperationStatus.Success, sensor.Tick(input, context));
        Assert.Equal(0, spatial.QueryCount);

        Assert.Equal(
            OperationStatus.Success,
            memory.FindByTypeAndRelated(MemoryType.TargetHeard, EntityId.FromInt32(4), out MemoryRecord record));
        Assert.Equal(Int2.Zero, record.Position);
        Assert.True((record.Flags & MemoryRecordFlags.UncertainPosition) != 0);
    }

    [Fact]
    public void DangerSensor_SetsImmediateInterruptFlag()
    {
        WorkingMemoryStore memory = new(8);
        PerceptionContext context = CreateContext(memory);
        context.AgentPosition = new Int2(0, 0);
        context.DangerInterruptRangeCells = 3;
        FakeDanger danger = new FakeDanger();
        danger.Add(new Int2(1, 1), intensity: 2);

        DangerSensor sensor = new DangerSensor();
        Assert.Equal(OperationStatus.Success, sensor.Tick(danger, context));
        Assert.True(context.ImmediateInterruptRequested);
        Assert.Equal(
            OperationStatus.Success,
            memory.TryGetAt(0, out MemoryRecord record));
        Assert.Equal(MemoryType.DangerDetected, record.Type);
        Assert.True((record.Flags & MemoryRecordFlags.ImmediateDanger) != 0);
    }

    private static PerceptionContext CreateContext(WorkingMemoryStore memory)
    {
        PerceptionContext context = new PerceptionContext(memory);
        context.BeginTick(
            AgentId.FromInt32(0),
            SquadId.Invalid,
            new Int2(0, 0),
            Direction8.East,
            new AiTick(1L, 16));
        return context;
    }

    private sealed class FakePerceptionInput : IPerceptionInput
    {
        private EntityId[] _visible = Array.Empty<EntityId>();
        private EntityId[] _heard = Array.Empty<EntityId>();
        private readonly EntityId[] _damage = Array.Empty<EntityId>();

        public void SetVisible(params EntityId[] entities) => _visible = entities;

        public void SetHeard(params EntityId[] entities) => _heard = entities;

        public OperationStatus CopyVisibleEntities(AgentId agent, Span<EntityId> destination, out int writtenCount)
        {
            return Copy(_visible, destination, out writtenCount);
        }

        public OperationStatus CopyHeardEntities(AgentId agent, Span<EntityId> destination, out int writtenCount)
        {
            return Copy(_heard, destination, out writtenCount);
        }

        public OperationStatus CopyDamageSources(AgentId agent, Span<EntityId> destination, out int writtenCount)
        {
            return Copy(_damage, destination, out writtenCount);
        }

        private static OperationStatus Copy(EntityId[] source, Span<EntityId> destination, out int writtenCount)
        {
            writtenCount = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (writtenCount >= destination.Length)
                {
                    return OperationStatus.CapacityExceeded;
                }

                destination[writtenCount] = source[i];
                writtenCount++;
            }

            return OperationStatus.Success;
        }
    }

    private sealed class FakeSpatial : ISpatialQueryService
    {
        private readonly EntityId[] _ids = new EntityId[8];
        private readonly Int2[] _positions = new Int2[8];
        private int _count;

        public void Set(EntityId id, Int2 position)
        {
            _ids[_count] = id;
            _positions[_count] = position;
            _count++;
        }

        public OperationStatus QueryEntitiesInRadius(Int2 origin, int radius, Span<EntityId> destination, out int writtenCount)
        {
            writtenCount = 0;
            return OperationStatus.Success;
        }

        public OperationStatus TryGetEntityPosition(EntityId entity, out Int2 position)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_ids[i] == entity)
                {
                    position = _positions[i];
                    return OperationStatus.Success;
                }
            }

            position = Int2.Zero;
            return OperationStatus.NotFound;
        }

        public DistanceCategory ClassifyDistance(Int2 from, Int2 destination)
        {
            int d = Int2.ManhattanDistance(from, destination);
            return d <= 4 ? DistanceCategory.Near : DistanceCategory.Far;
        }
    }

    private sealed class TrackingSpatial : ISpatialQueryService
    {
        public int QueryCount { get; private set; }

        public OperationStatus QueryEntitiesInRadius(Int2 origin, int radius, Span<EntityId> destination, out int writtenCount)
        {
            writtenCount = 0;
            QueryCount++;
            return OperationStatus.Success;
        }

        public OperationStatus TryGetEntityPosition(EntityId entity, out Int2 position)
        {
            QueryCount++;
            position = Int2.Zero;
            return OperationStatus.NotFound;
        }

        public DistanceCategory ClassifyDistance(Int2 from, Int2 destination) => DistanceCategory.Far;
    }

    private sealed class AlwaysClearLos : ILineOfSightService
    {
        public bool HasLineOfSight(Int2 from, Int2 destination) => true;

        public OperationStatus TryQueryLineOfSight(Int2 from, Int2 destination, out bool clear)
        {
            clear = true;
            return OperationStatus.Success;
        }
    }

    private sealed class FakeDanger : IDangerProvider
    {
        private readonly Int2[] _positions = new Int2[8];
        private readonly int[] _intensities = new int[8];
        private int _count;

        public void Add(Int2 position, int intensity)
        {
            _positions[_count] = position;
            _intensities[_count] = intensity;
            _count++;
        }

        public OperationStatus CopyDangerPositions(Span<Int2> destination, out int writtenCount)
        {
            writtenCount = 0;
            for (int i = 0; i < _count; i++)
            {
                if (writtenCount >= destination.Length)
                {
                    return OperationStatus.CapacityExceeded;
                }

                destination[writtenCount] = _positions[i];
                writtenCount++;
            }

            return OperationStatus.Success;
        }

        public bool IsPositionDangerous(Int2 position)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_positions[i].Equals(position))
                {
                    return true;
                }
            }

            return false;
        }

        public OperationStatus TryGetDangerIntensity(Int2 position, out int intensity)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_positions[i].Equals(position))
                {
                    intensity = _intensities[i];
                    return OperationStatus.Success;
                }
            }

            intensity = 0;
            return OperationStatus.NotFound;
        }
    }
}
