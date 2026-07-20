using System;
using System.Globalization;
using System.Text;

namespace TacticalGoap.Diagnostics.Formatting;

/// <summary>
/// Formats working-memory dump rows on demand.
/// </summary>
public static class MemoryDumpFormatter
{
    /// <summary>
    /// Formats memory dump rows.
    /// </summary>
    /// <param name="rows">Memory records to dump.</param>
    /// <returns>Allocated multi-line dump.</returns>
    public static string Format(ReadOnlySpan<MemoryDumpRow> rows)
    {
        if (rows.Length == 0)
        {
            return "memory: (empty)";
        }

        StringBuilder builder = new StringBuilder(64 + (rows.Length * 64));
        builder.Append(CultureInfo.InvariantCulture, $"memory count={rows.Length}");
        for (int i = 0; i < rows.Length; i++)
        {
            builder.AppendLine();
            MemoryDumpRow row = rows[i];
            builder.Append(
                CultureInfo.InvariantCulture,
                $"  id={row.RecordId} type={row.Type} src={row.SourceEntity} rel={row.RelatedEntity} pos=({row.PositionX},{row.PositionY}) conf={row.Confidence} flags={row.Flags} tick={row.UpdateTick}");
        }

        return builder.ToString();
    }
}
