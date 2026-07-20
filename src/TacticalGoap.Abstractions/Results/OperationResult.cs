using TacticalGoap.Abstractions.Enums;

namespace TacticalGoap.Abstractions.Results;

/// <summary>
/// Explicit operation outcome carrying status and bounded diagnostic payload.
/// </summary>
/// <remarks>
/// Runtime-critical paths return this value type instead of throwing for expected
/// failures. All fields are blittable integers or enums; constructing a result
/// never allocates. Callers must inspect <see cref="Status"/> before consuming
/// diagnostic identifiers.
/// </remarks>
public readonly struct OperationResult
{
    /// <summary>
    /// Initializes a new operation result.
    /// </summary>
    /// <param name="status">Operation outcome status.</param>
    /// <param name="subsystem">Subsystem that produced the result.</param>
    /// <param name="reasonCode">Subsystem-specific reason code; zero when unused.</param>
    /// <param name="primaryId">Primary diagnostic identifier; zero when unused.</param>
    /// <param name="secondaryId">Secondary diagnostic identifier; zero when unused.</param>
    /// <param name="valueA">First diagnostic integer payload; zero when unused.</param>
    /// <param name="valueB">Second diagnostic integer payload; zero when unused.</param>
    public OperationResult(
        OperationStatus status,
        DiagnosticSubsystem subsystem,
        int reasonCode,
        int primaryId,
        int secondaryId,
        int valueA,
        int valueB)
    {
        Status = status;
        Subsystem = subsystem;
        ReasonCode = reasonCode;
        PrimaryId = primaryId;
        SecondaryId = secondaryId;
        ValueA = valueA;
        ValueB = valueB;
    }

    /// <summary>
    /// Gets the operation outcome status.
    /// </summary>
    public OperationStatus Status { get; }

    /// <summary>
    /// Gets the subsystem that produced the result.
    /// </summary>
    public DiagnosticSubsystem Subsystem { get; }

    /// <summary>
    /// Gets the subsystem-specific reason code.
    /// </summary>
    public int ReasonCode { get; }

    /// <summary>
    /// Gets the primary diagnostic identifier.
    /// </summary>
    public int PrimaryId { get; }

    /// <summary>
    /// Gets the secondary diagnostic identifier.
    /// </summary>
    public int SecondaryId { get; }

    /// <summary>
    /// Gets the first diagnostic integer payload.
    /// </summary>
    public int ValueA { get; }

    /// <summary>
    /// Gets the second diagnostic integer payload.
    /// </summary>
    public int ValueB { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Status"/> is <see cref="OperationStatus.Success"/>.
    /// </summary>
    public bool IsSuccess => Status == OperationStatus.Success;

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    /// <param name="subsystem">Subsystem that produced the result.</param>
    /// <param name="primaryId">Optional primary diagnostic identifier.</param>
    /// <param name="secondaryId">Optional secondary diagnostic identifier.</param>
    /// <param name="valueA">Optional first diagnostic payload.</param>
    /// <param name="valueB">Optional second diagnostic payload.</param>
    /// <returns>A success result with zero reason code.</returns>
    public static OperationResult Success(
        DiagnosticSubsystem subsystem,
        int primaryId = 0,
        int secondaryId = 0,
        int valueA = 0,
        int valueB = 0)
    {
        return new OperationResult(
            OperationStatus.Success,
            subsystem,
            0,
            primaryId,
            secondaryId,
            valueA,
            valueB);
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="status">Non-success status describing the failure class.</param>
    /// <param name="subsystem">Subsystem that produced the result.</param>
    /// <param name="reasonCode">Subsystem-specific reason code.</param>
    /// <param name="primaryId">Optional primary diagnostic identifier.</param>
    /// <param name="secondaryId">Optional secondary diagnostic identifier.</param>
    /// <param name="valueA">Optional first diagnostic payload.</param>
    /// <param name="valueB">Optional second diagnostic payload.</param>
    /// <returns>A failure result using the provided status.</returns>
    public static OperationResult Failure(
        OperationStatus status,
        DiagnosticSubsystem subsystem,
        int reasonCode = 0,
        int primaryId = 0,
        int secondaryId = 0,
        int valueA = 0,
        int valueB = 0)
    {
        return new OperationResult(
            status,
            subsystem,
            reasonCode,
            primaryId,
            secondaryId,
            valueA,
            valueB);
    }
}
