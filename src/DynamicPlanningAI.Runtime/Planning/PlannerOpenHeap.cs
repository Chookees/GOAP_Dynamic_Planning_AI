using System;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.Planning;

/// <summary>
/// Fixed-capacity binary min-heap for planner open-set nodes.
/// </summary>
/// <remarks>
/// Capacity is fixed at construction and never grows. Tie-breaking is deterministic:
/// total cost (g+h), heuristic, insertion order, then node index. All loops are
/// bounded by capacity. No recursion is used.
/// </remarks>
public sealed class PlannerOpenHeap
{
    private readonly int[] _nodeIndices;
    private readonly PlannerNode[] _nodes;
    private readonly int _capacity;
    private int _count;

    /// <summary>
    /// Initializes a heap that indexes into a shared node table.
    /// </summary>
    /// <param name="capacity">Maximum open entries.</param>
    /// <param name="nodes">Shared planner node storage.</param>
    public PlannerOpenHeap(int capacity, PlannerNode[] nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        if (nodes.Length < capacity)
        {
            throw new ArgumentException("Node table must be at least as large as heap capacity.", nameof(nodes));
        }

        _capacity = capacity;
        _nodes = nodes;
        _nodeIndices = new int[capacity];
        _count = 0;
    }

    /// <summary>
    /// Gets the fixed capacity.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the current number of entries.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether the heap is empty.
    /// </summary>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Inserts a node index into the open set.
    /// </summary>
    /// <param name="nodeIndex">Index into the shared node table.</param>
    /// <returns>Success or capacity/validation failure.</returns>
    public OperationStatus Insert(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= _nodes.Length)
        {
            return OperationStatus.InvalidArgument;
        }

        if (_count >= _capacity)
        {
            return OperationStatus.CapacityExceeded;
        }

        int hole = _count;
        _count = checked(_count + 1);
        SiftUp(hole, nodeIndex);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Peeks at the minimum node index without removing it.
    /// </summary>
    /// <param name="nodeIndex">Receives the minimum node index.</param>
    /// <returns><see langword="true"/> when the heap is non-empty.</returns>
    public bool TryPeek(out int nodeIndex)
    {
        if (_count == 0)
        {
            nodeIndex = -1;
            return false;
        }

        nodeIndex = _nodeIndices[0];
        return true;
    }

    /// <summary>
    /// Removes and returns the minimum node index.
    /// </summary>
    /// <param name="nodeIndex">Receives the minimum node index.</param>
    /// <returns><see langword="true"/> when the heap was non-empty.</returns>
    public bool TryPop(out int nodeIndex)
    {
        if (_count == 0)
        {
            nodeIndex = -1;
            return false;
        }

        nodeIndex = _nodeIndices[0];
        _count--;
        if (_count > 0)
        {
            int last = _nodeIndices[_count];
            SiftDown(0, last);
        }

        return true;
    }

    /// <summary>
    /// Clears all entries without releasing capacity.
    /// </summary>
    public void Clear()
    {
        _count = 0;
    }

    private void SiftUp(int hole, int nodeIndex)
    {
        while (hole > 0)
        {
            int parent = checked((hole - 1) / 2);
            int parentIndex = _nodeIndices[parent];
            if (Compare(nodeIndex, parentIndex) >= 0)
            {
                break;
            }

            _nodeIndices[hole] = parentIndex;
            hole = parent;
        }

        _nodeIndices[hole] = nodeIndex;
    }

    private void SiftDown(int hole, int nodeIndex)
    {
        int half = _count / 2;
        while (hole < half)
        {
            int child = checked((hole * 2) + 1);
            int right = checked(child + 1);
            if (right < _count && Compare(_nodeIndices[right], _nodeIndices[child]) < 0)
            {
                child = right;
            }

            if (Compare(_nodeIndices[child], nodeIndex) >= 0)
            {
                break;
            }

            _nodeIndices[hole] = _nodeIndices[child];
            hole = child;
        }

        _nodeIndices[hole] = nodeIndex;
    }

    private int Compare(int leftNodeIndex, int rightNodeIndex)
    {
        ref PlannerNode left = ref _nodes[leftNodeIndex];
        ref PlannerNode right = ref _nodes[rightNodeIndex];

        int leftTotal = checked(left.GCost + left.HCost);
        int rightTotal = checked(right.GCost + right.HCost);
        int cmp = leftTotal.CompareTo(rightTotal);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = left.HCost.CompareTo(right.HCost);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = left.InsertionOrder.CompareTo(right.InsertionOrder);
        if (cmp != 0)
        {
            return cmp;
        }

        return leftNodeIndex.CompareTo(rightNodeIndex);
    }
}
