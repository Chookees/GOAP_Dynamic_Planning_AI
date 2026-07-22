using System;
using System.Globalization;
using System.Text;

namespace DynamicPlanningAI.Diagnostics.Formatting;

/// <summary>
/// Formats goal priority evaluation tables on demand.
/// </summary>
public static class GoalPriorityTableFormatter
{
    /// <summary>
    /// Formats a goal priority table.
    /// </summary>
    /// <param name="rows">Priority rows in evaluation order.</param>
    /// <returns>Allocated multi-line table.</returns>
    public static string Format(ReadOnlySpan<GoalPriorityRow> rows)
    {
        if (rows.Length == 0)
        {
            return "goals: (empty)";
        }

        StringBuilder builder = new StringBuilder(64 + (rows.Length * 40));
        builder.Append(CultureInfo.InvariantCulture, $"goals count={rows.Length}");
        for (int i = 0; i < rows.Length; i++)
        {
            builder.AppendLine();
            GoalPriorityRow row = rows[i];
            string selected = row.IsSelected ? "*" : " ";
            string relevant = row.IsRelevant ? "Y" : "N";
            builder.Append(CultureInfo.InvariantCulture, $"{selected} goal={row.GoalId} priority={row.Priority} relevant={relevant}");
        }

        return builder.ToString();
    }
}
