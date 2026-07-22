using System;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.Navigation;

/// <summary>
/// Fixed-capacity buffer of navigation path nodes owned by the caller or workspace.
/// </summary>
/// <remarks>
/// Capacity is fixed at construction. No allocations occur after the constructor.
/// </remarks>
public sealed class NavigationPathBuffer
{
    private readonly NavigationNodeId[] _nodes;
    private readonly int _capacity;
    private int _count;

    /// <summary>
    /// Initializes a path buffer sized to the hard navigation limit.
    /// </summary>
    public NavigationPathBuffer()
        : this(AiHardLimits.MaximumNavigationPathNodes)
    {
    }

    /// <summary>
    /// Initializes a path buffer with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Node capacity in 1..<see cref="AiHardLimits.MaximumNavigationPathNodes"/>.</param>
    public NavigationPathBuffer(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumNavigationPathNodes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be in 1..MaximumNavigationPathNodes.");
        }

        _capacity = capacity;
        _nodes = new NavigationNodeId[capacity];
        _count = 0;
    }

    /// <summary>
    /// Gets the fixed node capacity.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the number of valid path nodes currently stored.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether the buffer contains no nodes.
    /// </summary>
    public bool IsEmpty => _count == 0;

    /// <summary>
    /// Returns a span over the stored path nodes.
    /// </summary>
    /// <returns>Span of length <see cref="Count"/>.</returns>
    public Span<NavigationNodeId> AsSpan() => _nodes.AsSpan(0, _count);

    /// <summary>
    /// Returns a read-only span over the stored path nodes.
    /// </summary>
    /// <returns>Read-only span of length <see cref="Count"/>.</returns>
    public ReadOnlySpan<NavigationNodeId> AsReadOnlySpan() => _nodes.AsSpan(0, _count);

    /// <summary>
    /// Returns the writable destination span for host path queries.
    /// </summary>
    /// <returns>Full capacity span available for writing.</returns>
    public Span<NavigationNodeId> AsDestinationSpan() => _nodes.AsSpan(0, _capacity);

    /// <summary>
    /// Clears all stored nodes.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _count; i++)
        {
            _nodes[i] = NavigationNodeId.Invalid;
        }

        _count = 0;
    }

    /// <summary>
    /// Commits <paramref name="writtenCount"/> nodes previously written into the destination span.
    /// </summary>
    /// <param name="writtenCount">Number of nodes written by the host service.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus CommitWrittenCount(int writtenCount)
    {
        if (writtenCount < 0 || writtenCount > _capacity)
        {
            return OperationStatus.InvalidArgument;
        }

        for (int i = writtenCount; i < _count; i++)
        {
            _nodes[i] = NavigationNodeId.Invalid;
        }

        _count = writtenCount;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Attempts to read a node at a path index.
    /// </summary>
    /// <param name="index">Zero-based path index.</param>
    /// <param name="node">Receives the node when in range.</param>
    /// <returns>Success or not-found.</returns>
    public OperationStatus TryGetNode(int index, out NavigationNodeId node)
    {
        if (index < 0 || index >= _count)
        {
            node = NavigationNodeId.Invalid;
            return OperationStatus.NotFound;
        }

        node = _nodes[index];
        return OperationStatus.Success;
    }
}
