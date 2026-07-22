using System;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.Planning;

/// <summary>
/// Fixed-capacity open-addressed hash table for duplicate planner state detection.
/// </summary>
/// <remarks>
/// Entries are keyed by requirement-state hash. Collisions are resolved by linear
/// probing bounded by capacity. Capacity never grows after construction.
/// </remarks>
public sealed class PlannerClosedTable
{
    private const ulong EmptySentinel = 0xFFFFFFFFFFFFFFFFUL;

    private readonly ulong[] _hashes;
    private readonly int[] _bestGCosts;
    private readonly int _capacity;
    private int _count;

    /// <summary>
    /// Initializes a closed table with fixed capacity.
    /// </summary>
    /// <param name="capacity">Maximum distinct hashes.</param>
    public PlannerClosedTable(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        _capacity = capacity;
        _hashes = new ulong[capacity];
        _bestGCosts = new int[capacity];
        Clear();
    }

    /// <summary>
    /// Gets the fixed capacity.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the number of occupied slots.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Attempts to record a state hash with its path cost.
    /// </summary>
    /// <param name="stateHash">Requirement-state hash.</param>
    /// <param name="gCost">Path cost associated with the hash.</param>
    /// <param name="isDuplicate">
    /// Receives <see langword="true"/> when an equal or better prior entry exists.
    /// </param>
    /// <returns>Success, capacity exceeded, or validation failure.</returns>
    public OperationStatus TryInsert(ulong stateHash, int gCost, out bool isDuplicate)
    {
        isDuplicate = false;
        if (gCost < 0)
        {
            return OperationStatus.InvalidArgument;
        }

        int start = HashToIndex(stateHash);
        int index = start;

        for (int probe = 0; probe < _capacity; probe++)
        {
            ulong existing = _hashes[index];
            if (existing == EmptySentinel)
            {
                if (_count >= _capacity)
                {
                    return OperationStatus.CapacityExceeded;
                }

                _hashes[index] = stateHash;
                _bestGCosts[index] = gCost;
                _count = checked(_count + 1);
                return OperationStatus.Success;
            }

            if (existing == stateHash)
            {
                if (_bestGCosts[index] <= gCost)
                {
                    isDuplicate = true;
                    return OperationStatus.Success;
                }

                _bestGCosts[index] = gCost;
                return OperationStatus.Success;
            }

            index = checked(index + 1);
            if (index >= _capacity)
            {
                index = 0;
            }
        }

        return OperationStatus.CapacityExceeded;
    }

    /// <summary>
    /// Clears all entries without releasing capacity.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _capacity; i++)
        {
            _hashes[i] = EmptySentinel;
            _bestGCosts[i] = int.MaxValue;
        }

        _count = 0;
    }

    private int HashToIndex(ulong stateHash)
    {
        return (int)(stateHash % (ulong)_capacity);
    }
}
