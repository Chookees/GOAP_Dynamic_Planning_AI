using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Ticks;

namespace TacticalGoap.Abstractions.Diagnostics;

/// <summary>
/// Blittable diagnostic record written into the fixed-capacity trace ring buffer.
/// </summary>
/// <remarks>
/// Records must remain allocation-free. Event payloads use integer identifiers
/// only; string formatting is deferred to offline diagnostic tooling.
/// </remarks>
public readonly struct TraceRecord
{
    /// <summary>
    /// Initializes a new trace record.
    /// </summary>
    /// <param name="tick">Tick stamp when the event occurred.</param>
    /// <param name="agent">Agent associated with the event.</param>
    /// <param name="squad">Squad associated with the event.</param>
    /// <param name="subsystem">Subsystem that emitted the event.</param>
    /// <param name="eventCode">Stable event code.</param>
    /// <param name="primaryId">Primary diagnostic identifier.</param>
    /// <param name="secondaryId">Secondary diagnostic identifier.</param>
    /// <param name="valueA">First integer payload.</param>
    /// <param name="valueB">Second integer payload.</param>
    /// <param name="statusCode">Operation or planner status code.</param>
    public TraceRecord(
        AiTick tick,
        AgentId agent,
        SquadId squad,
        DiagnosticSubsystem subsystem,
        int eventCode,
        int primaryId,
        int secondaryId,
        int valueA,
        int valueB,
        int statusCode)
    {
        Tick = tick;
        Agent = agent;
        Squad = squad;
        Subsystem = subsystem;
        EventCode = eventCode;
        PrimaryId = primaryId;
        SecondaryId = secondaryId;
        ValueA = valueA;
        ValueB = valueB;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the tick stamp when the event occurred.
    /// </summary>
    public AiTick Tick { get; }

    /// <summary>
    /// Gets the agent associated with the event.
    /// </summary>
    public AgentId Agent { get; }

    /// <summary>
    /// Gets the squad associated with the event.
    /// </summary>
    public SquadId Squad { get; }

    /// <summary>
    /// Gets the subsystem that emitted the event.
    /// </summary>
    public DiagnosticSubsystem Subsystem { get; }

    /// <summary>
    /// Gets the stable event code, typically a <see cref="TraceEventCode"/> cast to <see cref="int"/>.
    /// </summary>
    public int EventCode { get; }

    /// <summary>
    /// Gets the primary diagnostic identifier.
    /// </summary>
    public int PrimaryId { get; }

    /// <summary>
    /// Gets the secondary diagnostic identifier.
    /// </summary>
    public int SecondaryId { get; }

    /// <summary>
    /// Gets the first integer payload.
    /// </summary>
    public int ValueA { get; }

    /// <summary>
    /// Gets the second integer payload.
    /// </summary>
    public int ValueB { get; }

    /// <summary>
    /// Gets the operation or planner status code.
    /// </summary>
    public int StatusCode { get; }
}
