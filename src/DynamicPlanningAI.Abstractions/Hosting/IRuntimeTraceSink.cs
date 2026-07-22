using DynamicPlanningAI.Abstractions.Diagnostics;

namespace DynamicPlanningAI.Abstractions.Hosting;

/// <summary>
/// Sink for blittable runtime diagnostic trace records.
/// </summary>
/// <remarks>
/// Implementations typically write into a fixed-capacity ring buffer sized by
/// <see cref="Limits.AiHardLimits.MaximumDiagnosticRecords"/>. Writes must not
/// allocate on the frozen runtime path. When the buffer is full, implementations
/// may overwrite the oldest record or drop the write; either policy must be
/// deterministic. The sink never retains a reference to caller stack memory
/// beyond the <see cref="Write"/> call.
/// </remarks>
public interface IRuntimeTraceSink
{
    /// <summary>
    /// Writes a diagnostic record into the sink.
    /// </summary>
    /// <param name="record">Blittable trace record.</param>
    public void Write(in TraceRecord record);
}
