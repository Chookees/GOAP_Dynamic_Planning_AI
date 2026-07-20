using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Memory;

/// <summary>
/// Fixed-capacity working-memory table for a single agent.
/// </summary>
/// <remarks>
/// Storage is a dense array allocated at construction. No per-record objects are
/// created after the constructor. Eviction is deterministic by priority then age.
/// Immediate-danger records are never evicted to make room for ambient evidence.
/// </remarks>
public sealed class WorkingMemoryStore
{
    private readonly MemoryRecord[] _records;
    private readonly int _capacity;
    private int _count;

    /// <summary>
    /// Initializes a store with hard-limit capacity.
    /// </summary>
    public WorkingMemoryStore()
        : this(AiHardLimits.MaximumMemoryRecords)
    {
    }

    /// <summary>
    /// Initializes a store with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Inclusive capacity in 1..<see cref="AiHardLimits.MaximumMemoryRecords"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is invalid.</exception>
    public WorkingMemoryStore(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumMemoryRecords)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be in 1..MaximumMemoryRecords.");
        }

        _capacity = capacity;
        _records = new MemoryRecord[capacity];
        for (int i = 0; i < capacity; i++)
        {
            _records[i].Clear();
        }

        _count = 0;
    }

    /// <summary>
    /// Gets the fixed capacity.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the number of occupied records.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Inserts a new record or updates an identity-matching live record.
    /// </summary>
    /// <param name="template">Candidate record fields (identifier ignored).</param>
    /// <returns>Write outcome with slot index on success.</returns>
    [FrozenRuntimePath]
    public MemoryWriteResult InsertOrUpdate(in MemoryRecord template)
    {
        int existing = FindMatchIndex(template);
        if (existing >= 0)
        {
            return UpdateAt(existing, template);
        }

        int free = FindFreeSlot();
        if (free >= 0)
        {
            return InsertAt(free, template);
        }

        return EvictAndInsert(template);
    }

    /// <summary>
    /// Looks up a live record by identifier.
    /// </summary>
    /// <param name="id">Record identifier.</param>
    /// <param name="record">Receives the record when found.</param>
    /// <returns>Success or not-found.</returns>
    [FrozenRuntimePath]
    public OperationStatus Lookup(MemoryRecordId id, out MemoryRecord record)
    {
        record = default;
        if (!id.IsValid || id.Value >= _capacity)
        {
            return OperationStatus.InvalidArgument;
        }

        ref MemoryRecord slot = ref _records[id.Value];
        if (!slot.IsOccupied || slot.Id != id)
        {
            return OperationStatus.NotFound;
        }

        record = slot;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Attempts to read a live record by slot index.
    /// </summary>
    /// <param name="slotIndex">Zero-based slot.</param>
    /// <param name="record">Receives the record when occupied.</param>
    /// <returns>Success or not-found.</returns>
    [FrozenRuntimePath]
    public OperationStatus TryGetAt(int slotIndex, out MemoryRecord record)
    {
        record = default;
        if (slotIndex < 0 || slotIndex >= _capacity)
        {
            return OperationStatus.InvalidArgument;
        }

        ref MemoryRecord slot = ref _records[slotIndex];
        if (!slot.IsOccupied)
        {
            return OperationStatus.NotFound;
        }

        record = slot;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Removes a live record by identifier.
    /// </summary>
    /// <param name="id">Record identifier.</param>
    /// <returns>Success, no-op, or validation failure.</returns>
    [FrozenRuntimePath]
    public OperationStatus Remove(MemoryRecordId id)
    {
        if (!id.IsValid || id.Value >= _capacity)
        {
            return OperationStatus.InvalidArgument;
        }

        ref MemoryRecord slot = ref _records[id.Value];
        if (!slot.IsOccupied || slot.Id != id)
        {
            return OperationStatus.NoOp;
        }

        slot.Clear();
        _count = checked(_count - 1);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Expires records whose expiration tick is less than or equal to <paramref name="currentTick"/>.
    /// </summary>
    /// <param name="currentTick">Current tick sequence.</param>
    /// <returns>Number of records removed.</returns>
    [FrozenRuntimePath]
    public int Expire(long currentTick)
    {
        int removed = 0;
        for (int i = 0; i < _capacity; i++)
        {
            ref MemoryRecord slot = ref _records[i];
            if (!slot.IsOccupied)
            {
                continue;
            }

            if (slot.ExpirationTick >= 0L && slot.ExpirationTick <= currentTick)
            {
                slot.Clear();
                _count = checked(_count - 1);
                removed = checked(removed + 1);
            }
        }

        return removed;
    }

    /// <summary>
    /// Decays confidence of all live records by <paramref name="amount"/>, removing zeros.
    /// </summary>
    /// <param name="amount">Non-negative decay amount.</param>
    /// <returns>Number of records removed due to zero confidence.</returns>
    [FrozenRuntimePath]
    public int DecayConfidence(int amount)
    {
        if (amount < 0)
        {
            return 0;
        }

        int removed = 0;
        for (int i = 0; i < _capacity; i++)
        {
            ref MemoryRecord slot = ref _records[i];
            if (!slot.IsOccupied)
            {
                continue;
            }

            int next = slot.Confidence - amount;
            if (next <= 0)
            {
                slot.Clear();
                _count = checked(_count - 1);
                removed = checked(removed + 1);
            }
            else
            {
                slot.Confidence = next;
            }
        }

        return removed;
    }

    /// <summary>
    /// Copies live records into <paramref name="destination"/> in ascending slot order.
    /// </summary>
    /// <param name="destination">Caller-owned destination span.</param>
    /// <param name="writtenCount">Receives the number of records written.</param>
    /// <returns>Success or capacity exceeded when destination is too small.</returns>
    [FrozenRuntimePath]
    public OperationStatus CopyTo(Span<MemoryRecord> destination, out int writtenCount)
    {
        writtenCount = 0;
        if (destination.Length < _count)
        {
            return OperationStatus.CapacityExceeded;
        }

        for (int i = 0; i < _capacity; i++)
        {
            ref MemoryRecord slot = ref _records[i];
            if (!slot.IsOccupied)
            {
                continue;
            }

            destination[writtenCount] = slot;
            writtenCount = checked(writtenCount + 1);
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Finds the first live record matching type and related entity.
    /// </summary>
    /// <param name="type">Memory type.</param>
    /// <param name="relatedEntity">Related entity.</param>
    /// <param name="record">Receives the record when found.</param>
    /// <returns>Success or not-found.</returns>
    [FrozenRuntimePath]
    public OperationStatus FindByTypeAndRelated(
        MemoryType type,
        EntityId relatedEntity,
        out MemoryRecord record)
    {
        record = default;
        for (int i = 0; i < _capacity; i++)
        {
            ref MemoryRecord slot = ref _records[i];
            if (!slot.IsOccupied)
            {
                continue;
            }

            if (slot.Type == type && slot.RelatedEntity == relatedEntity)
            {
                record = slot;
                return OperationStatus.Success;
            }
        }

        return OperationStatus.NotFound;
    }

    [FrozenRuntimePath]
    private MemoryWriteResult EvictAndInsert(in MemoryRecord template)
    {
        int victim = SelectEvictionVictim(template);
        if (victim < 0)
        {
            return MemoryWriteResult.Failure(OperationStatus.CapacityExceeded);
        }

        if (_records[victim].IsOccupied)
        {
            _records[victim].Clear();
            _count = checked(_count - 1);
        }

        return InsertAt(victim, template);
    }

    [FrozenRuntimePath]
    private int SelectEvictionVictim(in MemoryRecord template)
    {
        bool ambientInsert = MemoryPriority.IsAmbient(template.Type, template.Flags);
        int bestIndex = -1;
        int bestPriority = int.MaxValue;
        long bestUpdate = long.MaxValue;

        for (int i = 0; i < _capacity; i++)
        {
            ref MemoryRecord slot = ref _records[i];
            if (!slot.IsOccupied)
            {
                return i;
            }

            if (ambientInsert && MemoryPriority.IsImmediateDanger(slot.Type, slot.Flags))
            {
                continue;
            }

            int priority = MemoryPriority.Compute(slot.Type, slot.Flags);
            if (priority > bestPriority)
            {
                continue;
            }

            bool betterPriority = priority < bestPriority;
            bool betterAge = priority == bestPriority && slot.UpdateTick < bestUpdate;
            bool betterSlot = priority == bestPriority
                && slot.UpdateTick == bestUpdate
                && (bestIndex < 0 || i < bestIndex);
            if (betterPriority || betterAge || betterSlot)
            {
                bestPriority = priority;
                bestUpdate = slot.UpdateTick;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    [FrozenRuntimePath]
    private MemoryWriteResult InsertAt(int slotIndex, in MemoryRecord template)
    {
        ref MemoryRecord slot = ref _records[slotIndex];
        slot = template;
        slot.Id = MemoryRecordId.FromInt32(slotIndex);
        if (slot.ExpirationTick == 0L)
        {
            slot.ExpirationTick = -1L;
        }

        _count = checked(_count + 1);
        return MemoryWriteResult.Success(slot.Id, slotIndex);
    }

    [FrozenRuntimePath]
    private MemoryWriteResult UpdateAt(int slotIndex, in MemoryRecord template)
    {
        ref MemoryRecord slot = ref _records[slotIndex];
        long creation = slot.CreationTick;
        MemoryRecordId id = slot.Id;
        slot = template;
        slot.Id = id;
        slot.CreationTick = creation;
        if (slot.ExpirationTick == 0L)
        {
            slot.ExpirationTick = -1L;
        }

        return MemoryWriteResult.Success(id, slotIndex);
    }

    [FrozenRuntimePath]
    private int FindFreeSlot()
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (!_records[i].IsOccupied)
            {
                return i;
            }
        }

        return -1;
    }

    [FrozenRuntimePath]
    private int FindMatchIndex(in MemoryRecord template)
    {
        for (int i = 0; i < _capacity; i++)
        {
            ref MemoryRecord slot = ref _records[i];
            if (!slot.IsOccupied)
            {
                continue;
            }

            if (MatchesIdentity(slot, template))
            {
                return i;
            }
        }

        return -1;
    }

    [FrozenRuntimePath]
    private static bool MatchesIdentity(in MemoryRecord existing, in MemoryRecord candidate)
    {
        if (existing.Type != candidate.Type)
        {
            return false;
        }

        if (candidate.RelatedEntity.IsValid)
        {
            return existing.RelatedEntity == candidate.RelatedEntity;
        }

        if (candidate.SourceEntity.IsValid)
        {
            return existing.SourceEntity == candidate.SourceEntity
                && existing.Position.Equals(candidate.Position);
        }

        return existing.Position.Equals(candidate.Position);
    }
}
