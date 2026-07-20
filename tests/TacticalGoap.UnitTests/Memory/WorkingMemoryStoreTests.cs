using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Memory;
using Xunit;

namespace TacticalGoap.UnitTests.Memory;

public sealed class WorkingMemoryStoreTests
{
    [Fact]
    public void InsertOrUpdate_InsertsAndUpdatesByIdentity()
    {
        WorkingMemoryStore store = new(8);
        EntityId target = EntityId.FromInt32(3);
        MemoryRecord first = CreateRecord(MemoryType.TargetSeen, target, 10, 500, MemoryRecordFlags.Visible);
        MemoryWriteResult insert = store.InsertOrUpdate(first);
        Assert.True(insert.IsSuccess);
        Assert.Equal(1, store.Count);

        MemoryRecord updated = CreateRecord(MemoryType.TargetSeen, target, 11, 900, MemoryRecordFlags.Visible);
        MemoryWriteResult write = store.InsertOrUpdate(updated);
        Assert.True(write.IsSuccess);
        Assert.Equal(insert.RecordId, write.RecordId);
        Assert.Equal(1, store.Count);

        Assert.Equal(OperationStatus.Success, store.Lookup(write.RecordId, out MemoryRecord live));
        Assert.Equal(11L, live.UpdateTick);
        Assert.Equal(900, live.Confidence);
        Assert.Equal(10L, live.CreationTick);
    }

    [Fact]
    public void Expire_RemovesElapsedRecords()
    {
        WorkingMemoryStore store = new(4);
        MemoryRecord record = CreateRecord(MemoryType.TargetHeard, EntityId.FromInt32(1), 5, 400, MemoryRecordFlags.UncertainPosition);
        record.ExpirationTick = 10L;
        store.InsertOrUpdate(record);

        Assert.Equal(0, store.Expire(9L));
        Assert.Equal(1, store.Count);
        Assert.Equal(1, store.Expire(10L));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void DecayConfidence_RemovesZeroConfidence()
    {
        WorkingMemoryStore store = new(4);
        MemoryRecord record = CreateRecord(MemoryType.TargetSeen, EntityId.FromInt32(2), 1, 50, MemoryRecordFlags.Visible);
        store.InsertOrUpdate(record);

        Assert.Equal(0, store.DecayConfidence(40));
        Assert.Equal(OperationStatus.Success, store.TryGetAt(0, out MemoryRecord live));
        Assert.Equal(10, live.Confidence);
        Assert.Equal(1, store.DecayConfidence(10));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void Eviction_PreferOldestLowestPriority()
    {
        WorkingMemoryStore store = new(2);
        store.InsertOrUpdate(CreateRecord(MemoryType.SearchSectorInspected, EntityId.Invalid, 1, 100, MemoryRecordFlags.Ambient));
        store.InsertOrUpdate(CreateRecord(MemoryType.TargetHeard, EntityId.FromInt32(4), 2, 200, MemoryRecordFlags.UncertainPosition));

        MemoryWriteResult write = store.InsertOrUpdate(
            CreateRecord(MemoryType.TargetSeen, EntityId.FromInt32(5), 3, 800, MemoryRecordFlags.Visible));
        Assert.True(write.IsSuccess);
        Assert.Equal(2, store.Count);

        MemoryRecord[] buffer = new MemoryRecord[2];
        Assert.Equal(OperationStatus.Success, store.CopyTo(buffer, out int written));
        Assert.Equal(2, written);
        for (int i = 0; i < written; i++)
        {
            Assert.NotEqual(MemoryType.SearchSectorInspected, buffer[i].Type);
        }
    }

    [Fact]
    public void Eviction_NeverEvictsImmediateDangerForAmbient()
    {
        WorkingMemoryStore store = new(1);
        MemoryRecord danger = CreateRecord(
            MemoryType.DangerDetected,
            EntityId.Invalid,
            1,
            1000,
            MemoryRecordFlags.ImmediateDanger);
        danger.Position = new Int2(4, 4);
        Assert.True(store.InsertOrUpdate(danger).IsSuccess);

        MemoryRecord ambient = CreateRecord(
            MemoryType.SearchSectorInspected,
            EntityId.Invalid,
            2,
            10,
            MemoryRecordFlags.Ambient);
        ambient.Position = new Int2(1, 1);
        MemoryWriteResult write = store.InsertOrUpdate(ambient);
        Assert.Equal(OperationStatus.CapacityExceeded, write.Status);
        Assert.Equal(1, store.Count);
        Assert.Equal(OperationStatus.Success, store.TryGetAt(0, out MemoryRecord live));
        Assert.Equal(MemoryType.DangerDetected, live.Type);
    }

    [Fact]
    public void Capacity_And_Remove_Work()
    {
        WorkingMemoryStore store = new(2);
        MemoryWriteResult a = store.InsertOrUpdate(
            CreateRecord(MemoryType.TargetSeen, EntityId.FromInt32(1), 1, 100, MemoryRecordFlags.Visible));
        MemoryWriteResult b = store.InsertOrUpdate(
            CreateRecord(MemoryType.TargetSeen, EntityId.FromInt32(2), 1, 100, MemoryRecordFlags.Visible));
        Assert.True(a.IsSuccess);
        Assert.True(b.IsSuccess);
        Assert.Equal(2, store.Count);

        Assert.Equal(OperationStatus.Success, store.Remove(a.RecordId));
        Assert.Equal(1, store.Count);
        Assert.Equal(OperationStatus.NoOp, store.Remove(a.RecordId));
    }

    [Fact]
    public void CopyTo_IsStableBySlotOrder()
    {
        WorkingMemoryStore store = new(4);
        store.InsertOrUpdate(CreateRecord(MemoryType.TargetSeen, EntityId.FromInt32(9), 1, 1, MemoryRecordFlags.Visible));
        store.InsertOrUpdate(CreateRecord(MemoryType.TargetSeen, EntityId.FromInt32(2), 1, 1, MemoryRecordFlags.Visible));
        store.InsertOrUpdate(CreateRecord(MemoryType.TargetSeen, EntityId.FromInt32(5), 1, 1, MemoryRecordFlags.Visible));

        MemoryRecord[] first = new MemoryRecord[3];
        MemoryRecord[] second = new MemoryRecord[3];
        store.CopyTo(first, out _);
        store.CopyTo(second, out _);
        Assert.Equal(first[0].RelatedEntity, second[0].RelatedEntity);
        Assert.Equal(first[1].RelatedEntity, second[1].RelatedEntity);
        Assert.Equal(first[2].RelatedEntity, second[2].RelatedEntity);
        Assert.True(first[0].Id.Value < first[1].Id.Value);
        Assert.True(first[1].Id.Value < first[2].Id.Value);
    }

    private static MemoryRecord CreateRecord(
        MemoryType type,
        EntityId related,
        long tick,
        int confidence,
        MemoryRecordFlags flags)
    {
        return new MemoryRecord
        {
            Type = type,
            SourceEntity = EntityId.FromInt32(0),
            RelatedEntity = related,
            Position = Int2.Zero,
            NavigationNode = NavigationNodeId.Invalid,
            CreationTick = tick,
            UpdateTick = tick,
            ExpirationTick = -1L,
            Confidence = confidence,
            Flags = flags,
        };
    }
}
