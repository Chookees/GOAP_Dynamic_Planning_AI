using System;
using System.Globalization;
using System.Text;

namespace TacticalGoap.Diagnostics.Formatting;

/// <summary>
/// Formats compact squad state snapshots on demand.
/// </summary>
public static class SquadStateFormatter
{
    /// <summary>
    /// Formats squad state rows.
    /// </summary>
    /// <param name="rows">Squad snapshots.</param>
    /// <returns>Allocated multi-line dump.</returns>
    public static string Format(ReadOnlySpan<SquadStateRow> rows)
    {
        if (rows.Length == 0)
        {
            return "squads: (empty)";
        }

        StringBuilder builder = new StringBuilder(64 + (rows.Length * 48));
        builder.Append(CultureInfo.InvariantCulture, $"squads count={rows.Length}");
        for (int i = 0; i < rows.Length; i++)
        {
            builder.AppendLine();
            SquadStateRow row = rows[i];
            string active = row.IsActive ? "active" : "inactive";
            builder.Append(
                CultureInfo.InvariantCulture,
                $"  squad={row.SquadId} {active} behavior={row.BehaviorCode} members={row.MemberCount} orders={row.ActiveOrderCount} focus=({row.FocusX},{row.FocusY})");
        }

        return builder.ToString();
    }
}
