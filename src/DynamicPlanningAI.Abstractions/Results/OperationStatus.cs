namespace DynamicPlanningAI.Abstractions.Results;

/// <summary>
/// Generic operation outcome used across runtime subsystem boundaries.
/// </summary>
/// <remarks>
/// Runtime-critical paths return statuses instead of throwing for expected
/// failures. Callers must check every non-void result. Success statuses are
/// clustered near zero; capacity and validation failures are explicit.
/// </remarks>
public enum OperationStatus : byte
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The operation completed with no work required.
    /// </summary>
    NoOp = 1,

    /// <summary>
    /// The operation is still in progress and should be polled again.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// A required parameter was invalid.
    /// </summary>
    InvalidArgument = 10,

    /// <summary>
    /// The runtime lifecycle state does not permit the operation.
    /// </summary>
    InvalidLifecycleState = 11,

    /// <summary>
    /// A fixed-capacity buffer or table is full.
    /// </summary>
    CapacityExceeded = 12,

    /// <summary>
    /// The requested resource was not found.
    /// </summary>
    NotFound = 13,

    /// <summary>
    /// The operation conflicted with an existing reservation or ownership.
    /// </summary>
    Conflict = 14,

    /// <summary>
    /// The operation was rejected by a host service.
    /// </summary>
    HostRejected = 15,

    /// <summary>
    /// The operation timed out within its configured bound.
    /// </summary>
    TimedOut = 16,

    /// <summary>
    /// The operation was cancelled by a higher-priority concern.
    /// </summary>
    Cancelled = 17,

    /// <summary>
    /// A contract or invariant check failed.
    /// </summary>
    ContractViolation = 18,

    /// <summary>
    /// Arithmetic overflow was detected during checked cost or index math.
    /// </summary>
    ArithmeticOverflow = 19,

    /// <summary>
    /// The operation failed for an unclassified but explicit reason.
    /// </summary>
    Failed = 255,
}
