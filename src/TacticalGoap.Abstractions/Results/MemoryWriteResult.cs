using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Abstractions.Results;

/// <summary>
/// Explicit working-memory write outcome.
/// </summary>
/// <remarks>
/// Callers must inspect <see cref="Status"/> before consuming
/// <see cref="RecordId"/>. Capacity failures leave existing records intact.
/// </remarks>
public readonly struct MemoryWriteResult
{
    /// <summary>
    /// Initializes a new memory write result.
    /// </summary>
    /// <param name="status">Write outcome status.</param>
    /// <param name="recordId">Written or updated record identifier when successful.</param>
    /// <param name="slotIndex">Zero-based memory slot; negative when unused.</param>
    public MemoryWriteResult(OperationStatus status, MemoryRecordId recordId, int slotIndex)
    {
        Status = status;
        RecordId = recordId;
        SlotIndex = slotIndex;
    }

    /// <summary>
    /// Gets the write outcome status.
    /// </summary>
    public OperationStatus Status { get; }

    /// <summary>
    /// Gets the written or updated record identifier when successful.
    /// </summary>
    public MemoryRecordId RecordId { get; }

    /// <summary>
    /// Gets the zero-based memory slot; negative when unused.
    /// </summary>
    public int SlotIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the write succeeded.
    /// </summary>
    public bool IsSuccess => Status == OperationStatus.Success;

    /// <summary>
    /// Creates a successful memory write result.
    /// </summary>
    /// <param name="recordId">Written record identifier.</param>
    /// <param name="slotIndex">Memory slot index.</param>
    /// <returns>A success result.</returns>
    public static MemoryWriteResult Success(MemoryRecordId recordId, int slotIndex)
    {
        return new MemoryWriteResult(OperationStatus.Success, recordId, slotIndex);
    }

    /// <summary>
    /// Creates a failed memory write result.
    /// </summary>
    /// <param name="status">Non-success status.</param>
    /// <returns>A failure result with an invalid record identifier.</returns>
    public static MemoryWriteResult Failure(OperationStatus status)
    {
        return new MemoryWriteResult(status, MemoryRecordId.Invalid, -1);
    }
}
