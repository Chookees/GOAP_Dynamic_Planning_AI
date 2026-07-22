using System;
using System.Globalization;
using System.Text;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Diagnostics.Formatting;

/// <summary>
/// Formats planner explanation rows into human-readable text on demand.
/// </summary>
public static class PlannerExplanationFormatter
{
    /// <summary>
    /// Formats a planner result header plus optional step rows.
    /// </summary>
    /// <param name="result">Planner step result.</param>
    /// <param name="steps">Optional explanation steps.</param>
    /// <returns>Allocated explanation text.</returns>
    public static string Format(in PlannerResult result, ReadOnlySpan<PlannerExplanationRow> steps)
    {
        StringBuilder builder = new StringBuilder(128 + (steps.Length * 48));
        AppendHeader(builder, result);
        for (int i = 0; i < steps.Length; i++)
        {
            builder.AppendLine();
            AppendStep(builder, steps[i]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats only the step rows.
    /// </summary>
    /// <param name="steps">Explanation steps.</param>
    /// <returns>Allocated multi-line text.</returns>
    public static string FormatSteps(ReadOnlySpan<PlannerExplanationRow> steps)
    {
        if (steps.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(steps.Length * 48);
        for (int i = 0; i < steps.Length; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            AppendStep(builder, steps[i]);
        }

        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, in PlannerResult result)
    {
        builder.Append(CultureInfo.InvariantCulture, $"planner status={result.Status} goal={result.GoalId} expansions={result.ExpansionsPerformed} nodes={result.NodesAllocated} planLen={result.PlanLength} cost={result.TotalCost}");
    }

    private static void AppendStep(StringBuilder builder, in PlannerExplanationRow step)
    {
        ActionId action = step.ActionId;
        builder.Append(CultureInfo.InvariantCulture, $"  step={step.StepIndex} action={action} cost={step.Cost} status={step.Status} note={step.NoteCode}");
    }
}
