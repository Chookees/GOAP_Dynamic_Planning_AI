using System;
using System.Globalization;
using System.Text;
using TacticalGoap.Abstractions.Diagnostics;
using TacticalGoap.Abstractions.Enums;

namespace TacticalGoap.Diagnostics.Formatting;

/// <summary>
/// Formats <see cref="TraceRecord"/> values into human-readable strings on demand.
/// </summary>
/// <remarks>
/// Allocation is intentional and allowed only when the host requests a dump.
/// Never call from <c>[FrozenRuntimePath]</c> tick code.
/// </remarks>
public static class TraceFormatter
{
    /// <summary>
    /// Formats a single trace record as one diagnostic line.
    /// </summary>
    /// <param name="record">Record to format.</param>
    /// <returns>Allocated diagnostic line.</returns>
    public static string Format(in TraceRecord record)
    {
        string eventName = ResolveEventName(record.EventCode);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"t={record.Tick.Sequence} dt={record.Tick.DeltaMilliseconds} agent={record.Agent} squad={record.Squad} sub={record.Subsystem} evt={eventName}({record.EventCode}) p={record.PrimaryId} s={record.SecondaryId} a={record.ValueA} b={record.ValueB} st={record.StatusCode}");
    }

    /// <summary>
    /// Formats a span of records into a multi-line dump.
    /// </summary>
    /// <param name="records">Records in oldest-to-newest order.</param>
    /// <returns>Allocated multi-line dump.</returns>
    public static string FormatAll(ReadOnlySpan<TraceRecord> records)
    {
        if (records.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(records.Length * 96);
        for (int i = 0; i < records.Length; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            builder.Append(Format(records[i]));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats all readable records from a ring buffer.
    /// </summary>
    /// <param name="buffer">Ring buffer to dump.</param>
    /// <returns>Allocated multi-line dump.</returns>
    public static string FormatRing(DiagnosticRingBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(buffer.Count * 96);
        for (int i = 0; i < buffer.Count; i++)
        {
            if (!buffer.TryRead(i, out TraceRecord record))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(Format(record));
        }

        if (buffer.DroppedCount > 0)
        {
            builder.AppendLine();
            builder.Append(CultureInfo.InvariantCulture, $"dropped={buffer.DroppedCount}");
        }

        return builder.ToString();
    }

    private static string ResolveEventName(int eventCode)
    {
        if (eventCode < 0 || eventCode > byte.MaxValue)
        {
            return "Unknown";
        }

        TraceEventCode code = (TraceEventCode)(byte)eventCode;
        return Enum.IsDefined(code) ? code.ToString() : "Unknown";
    }
}
