namespace DynamicPlanningAI.Abstractions.Results;

/// <summary>
/// Outcome of a host navigation path query.
/// </summary>
public enum NavigationQueryStatus : byte
{
    /// <summary>
    /// A complete path was written into the caller buffer.
    /// </summary>
    Success = 0,

    /// <summary>
    /// No walkable path exists between the query endpoints.
    /// </summary>
    NoPath = 1,

    /// <summary>
    /// The path would exceed the caller buffer or hard navigation capacity.
    /// </summary>
    CapacityExceeded = 2,

    /// <summary>
    /// The query arguments were invalid.
    /// </summary>
    InvalidQuery = 3,

    /// <summary>
    /// The host navigation service rejected the query.
    /// </summary>
    HostRejected = 4,

    /// <summary>
    /// A truncated path was written; the destination was not fully reached.
    /// </summary>
    PartialPath = 5,
}

/// <summary>
/// Explicit navigation query result carrying status and path length metadata.
/// </summary>
/// <remarks>
/// Callers must inspect <see cref="Status"/> before consuming path nodes written
/// into the destination span. Partial paths remain usable for approach behavior
/// when <see cref="Status"/> is <see cref="NavigationQueryStatus.PartialPath"/>.
/// </remarks>
public readonly struct NavigationQueryResult
{
    /// <summary>
    /// Initializes a new navigation query result.
    /// </summary>
    /// <param name="status">Query outcome status.</param>
    /// <param name="pathLength">Number of nodes written into the caller buffer.</param>
    /// <param name="estimatedRemainingCost">Host-reported remaining cost for partial paths; otherwise zero.</param>
    public NavigationQueryResult(NavigationQueryStatus status, int pathLength, int estimatedRemainingCost)
    {
        Status = status;
        PathLength = pathLength;
        EstimatedRemainingCost = estimatedRemainingCost;
    }

    /// <summary>
    /// Gets the query outcome status.
    /// </summary>
    public NavigationQueryStatus Status { get; }

    /// <summary>
    /// Gets the number of path nodes written into the caller-provided buffer.
    /// </summary>
    public int PathLength { get; }

    /// <summary>
    /// Gets the host-reported remaining cost for partial paths; otherwise zero.
    /// </summary>
    public int EstimatedRemainingCost { get; }

    /// <summary>
    /// Gets a value indicating whether a complete path was produced.
    /// </summary>
    public bool IsSuccess => Status == NavigationQueryStatus.Success;

    /// <summary>
    /// Gets a value indicating whether any path nodes were written.
    /// </summary>
    public bool HasPath =>
        (Status == NavigationQueryStatus.Success || Status == NavigationQueryStatus.PartialPath)
        && PathLength > 0;

    /// <summary>
    /// Creates a successful navigation result.
    /// </summary>
    /// <param name="pathLength">Number of nodes written.</param>
    /// <returns>A success result with zero remaining cost.</returns>
    public static NavigationQueryResult Success(int pathLength)
    {
        return new NavigationQueryResult(NavigationQueryStatus.Success, pathLength, 0);
    }

    /// <summary>
    /// Creates a failed or partial navigation result.
    /// </summary>
    /// <param name="status">Non-success status.</param>
    /// <param name="pathLength">Nodes written when partial; otherwise zero.</param>
    /// <param name="estimatedRemainingCost">Remaining cost for partial paths.</param>
    /// <returns>A result with the provided status.</returns>
    public static NavigationQueryResult Failure(
        NavigationQueryStatus status,
        int pathLength = 0,
        int estimatedRemainingCost = 0)
    {
        return new NavigationQueryResult(status, pathLength, estimatedRemainingCost);
    }
}
