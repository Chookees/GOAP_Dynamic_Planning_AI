using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Sample.World;

namespace DynamicPlanningAI.Sample.Navigation;

/// <summary>
/// Iterative A* grid navigation with preallocated storage and bounded expansion.
/// </summary>
/// <remarks>
/// No recursion. Capacity and no-path outcomes are explicit. Storage is allocated
/// at construction and reused for every query after freeze.
/// </remarks>
public sealed class GridNavigationService : INavigationService
{
    private readonly GridMap _map;
    private readonly int _maxExpansions;
    private readonly int _maxPathNodes;
    private readonly int[] _openHeap;
    private readonly int[] _gScore;
    private readonly int[] _cameFrom;
    private readonly int[] _closedStamp;
    private readonly int[] _openStamp;
    private int _stamp;
    private int _openCount;
    private int _activeGoalIndex;
    private bool _allowWindows;

    /// <summary>
    /// Initializes navigation storage for a map.
    /// </summary>
    /// <param name="map">Grid map.</param>
    /// <param name="maxExpansions">Bounded expansion budget.</param>
    /// <param name="maxPathNodes">Maximum nodes written to caller buffers.</param>
    public GridNavigationService(GridMap map, int maxExpansions, int maxPathNodes)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (maxExpansions < 1 || maxExpansions > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExpansions));
        }

        if (maxPathNodes < 1 || maxPathNodes > AiHardLimits.MaximumNavigationPathNodes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPathNodes));
        }

        _map = map;
        _maxExpansions = maxExpansions;
        _maxPathNodes = maxPathNodes;
        int cells = map.CellCount;
        _openHeap = new int[cells];
        _gScore = new int[cells];
        _cameFrom = new int[cells];
        _closedStamp = new int[cells];
        _openStamp = new int[cells];
        _stamp = 1;
        _openCount = 0;
        _allowWindows = true;
    }

    /// <summary>
    /// Gets or sets whether window cells are traversable.
    /// </summary>
    public bool AllowWindows
    {
        get => _allowWindows;
        set => _allowWindows = value;
    }

    /// <inheritdoc />
    [FrozenRuntimePath]
    public NavigationQueryResult TryFindPath(
        in NavigationPathQuery query,
        Span<NavigationNodeId> destination,
        out int writtenCount)
    {
        writtenCount = 0;
        if (destination.Length < 1)
        {
            return NavigationQueryResult.Failure(NavigationQueryStatus.InvalidQuery);
        }

        Int2 origin = ResolveCell(query.Origin, query.OriginCell);
        Int2 goal = ResolveCell(query.Destination, query.DestinationCell);
        if (!_map.InBounds(origin) || !_map.InBounds(goal))
        {
            return NavigationQueryResult.Failure(NavigationQueryStatus.InvalidQuery);
        }

        if (!_map.IsWalkable(origin, _allowWindows) || !_map.IsWalkable(goal, _allowWindows))
        {
            return NavigationQueryResult.Failure(NavigationQueryStatus.NoPath);
        }

        int originIndex = _map.ToIndex(origin);
        int goalIndex = _map.ToIndex(goal);
        if (originIndex == goalIndex)
        {
            destination[0] = NavigationNodeId.FromInt32(originIndex);
            writtenCount = 1;
            return NavigationQueryResult.Success(1);
        }

        _activeGoalIndex = goalIndex;
        BeginSearch(originIndex);
        return Search(originIndex, goalIndex, query.AllowPartial, destination, out writtenCount);
    }

    [FrozenRuntimePath]
    private NavigationQueryResult Search(
        int originIndex,
        int goalIndex,
        bool allowPartial,
        Span<NavigationNodeId> destination,
        out int writtenCount)
    {
        writtenCount = 0;
        int expansions = 0;
        int bestIndex = originIndex;
        int bestHeuristic = Heuristic(originIndex, goalIndex);

        for (int guard = 0; guard < _maxExpansions && _openCount > 0; guard++)
        {
            int current = PopOpen();
            if (_closedStamp[current] == _stamp)
            {
                continue;
            }

            expansions = checked(expansions + 1);
            if (current == goalIndex)
            {
                return Reconstruct(current, destination, out writtenCount);
            }

            _closedStamp[current] = _stamp;
            ExpandNeighbors(current, goalIndex, ref bestIndex, ref bestHeuristic);

            if (expansions >= _maxExpansions)
            {
                break;
            }
        }

        if (allowPartial && bestIndex != originIndex)
        {
            NavigationQueryResult partial = Reconstruct(bestIndex, destination, out writtenCount);
            if (partial.HasPath)
            {
                return NavigationQueryResult.Failure(
                    NavigationQueryStatus.PartialPath,
                    writtenCount,
                    Heuristic(bestIndex, goalIndex));
            }
        }

        if (expansions >= _maxExpansions && _openCount > 0)
        {
            return NavigationQueryResult.Failure(NavigationQueryStatus.CapacityExceeded);
        }

        return NavigationQueryResult.Failure(NavigationQueryStatus.NoPath);
    }

    [FrozenRuntimePath]
    private void ExpandNeighbors(int current, int goalIndex, ref int bestIndex, ref int bestHeuristic)
    {
        Int2 cell = _map.ToCell(current);
        TryNeighbor(current, cell.X + 1, cell.Y, goalIndex, ref bestIndex, ref bestHeuristic);
        TryNeighbor(current, cell.X - 1, cell.Y, goalIndex, ref bestIndex, ref bestHeuristic);
        TryNeighbor(current, cell.X, cell.Y + 1, goalIndex, ref bestIndex, ref bestHeuristic);
        TryNeighbor(current, cell.X, cell.Y - 1, goalIndex, ref bestIndex, ref bestHeuristic);
    }

    [FrozenRuntimePath]
    private void TryNeighbor(
        int current,
        int x,
        int y,
        int goalIndex,
        ref int bestIndex,
        ref int bestHeuristic)
    {
        Int2 next = new(x, y);
        if (!_map.IsWalkable(next, _allowWindows))
        {
            return;
        }

        int nextIndex = _map.ToIndex(next);
        if (_closedStamp[nextIndex] == _stamp)
        {
            return;
        }

        int stepCost = _map.GetKind(next) == CellKind.Window ? 2 : 1;
        int tentative = checked(_gScore[current] + stepCost);
        if (_openStamp[nextIndex] == _stamp && tentative >= _gScore[nextIndex])
        {
            return;
        }

        _cameFrom[nextIndex] = current;
        _gScore[nextIndex] = tentative;
        PushOpen(nextIndex);

        int h = Heuristic(nextIndex, goalIndex);
        if (h < bestHeuristic)
        {
            bestHeuristic = h;
            bestIndex = nextIndex;
        }
    }

    [FrozenRuntimePath]
    private NavigationQueryResult Reconstruct(
        int endIndex,
        Span<NavigationNodeId> destination,
        out int writtenCount)
    {
        writtenCount = 0;
        int[] reverse = _openHeap;
        int count = 0;
        int cursor = endIndex;
        for (int i = 0; i < _maxPathNodes && i < AiHardLimits.MaximumNavigationPathNodes; i++)
        {
            reverse[count] = cursor;
            count = checked(count + 1);
            int parent = _cameFrom[cursor];
            if (parent < 0 || parent == cursor)
            {
                break;
            }

            cursor = parent;
        }

        if (count > destination.Length || count > _maxPathNodes)
        {
            return NavigationQueryResult.Failure(NavigationQueryStatus.CapacityExceeded);
        }

        for (int i = 0; i < count; i++)
        {
            destination[i] = NavigationNodeId.FromInt32(reverse[count - 1 - i]);
        }

        writtenCount = count;
        return NavigationQueryResult.Success(count);
    }

    [FrozenRuntimePath]
    private void BeginSearch(int originIndex)
    {
        _stamp = checked(_stamp + 1);
        if (_stamp == int.MaxValue)
        {
            Array.Clear(_closedStamp, 0, _closedStamp.Length);
            Array.Clear(_openStamp, 0, _openStamp.Length);
            _stamp = 1;
        }

        _openCount = 0;
        int cellBound = _gScore.Length;
        for (int i = 0; i < cellBound; i++)
        {
            _gScore[i] = int.MaxValue / 4;
            _cameFrom[i] = -1;
        }

        _gScore[originIndex] = 0;
        _cameFrom[originIndex] = originIndex;
        PushOpen(originIndex);
    }

    [FrozenRuntimePath]
    private void PushOpen(int index)
    {
        _openStamp[index] = _stamp;
        _openHeap[_openCount] = index;
        _openCount = checked(_openCount + 1);
        int child = checked(_openCount - 1);
        while (child > 0)
        {
            int parent = (child - 1) / 2;
            if (Compare(_openHeap[child], _openHeap[parent]) >= 0)
            {
                break;
            }

            int tmp = _openHeap[child];
            _openHeap[child] = _openHeap[parent];
            _openHeap[parent] = tmp;
            child = parent;
        }
    }

    [FrozenRuntimePath]
    private int PopOpen()
    {
        int root = _openHeap[0];
        _openCount = checked(_openCount - 1);
        if (_openCount > 0)
        {
            _openHeap[0] = _openHeap[_openCount];
            SiftDown(0);
        }

        return root;
    }

    [FrozenRuntimePath]
    private void SiftDown(int parent)
    {
        for (int guard = 0; guard < _maxExpansions; guard++)
        {
            int left = checked((parent * 2) + 1);
            if (left >= _openCount)
            {
                return;
            }

            int right = checked(left + 1);
            int best = left;
            if (right < _openCount && Compare(_openHeap[right], _openHeap[left]) < 0)
            {
                best = right;
            }

            if (Compare(_openHeap[parent], _openHeap[best]) <= 0)
            {
                return;
            }

            int tmp = _openHeap[parent];
            _openHeap[parent] = _openHeap[best];
            _openHeap[best] = tmp;
            parent = best;
        }
    }

    [FrozenRuntimePath]
    private int Compare(int a, int b)
    {
        int fa = checked(_gScore[a] + Heuristic(a, _activeGoalIndex));
        int fb = checked(_gScore[b] + Heuristic(b, _activeGoalIndex));
        if (fa != fb)
        {
            return fa.CompareTo(fb);
        }

        return a.CompareTo(b);
    }

    [FrozenRuntimePath]
    private int Heuristic(int fromIndex, int toIndex)
    {
        Int2 a = _map.ToCell(fromIndex);
        Int2 b = _map.ToCell(toIndex);
        return Int2.ManhattanDistance(a, b);
    }

    private Int2 ResolveCell(NavigationNodeId node, Int2 fallback)
    {
        if (node.IsValid && node.Value < _map.CellCount)
        {
            return _map.ToCell(node.Value);
        }

        return fallback;
    }
}
