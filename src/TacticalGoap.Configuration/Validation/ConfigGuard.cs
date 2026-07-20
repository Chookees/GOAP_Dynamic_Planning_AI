using System;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Configuration.Validation;

/// <summary>
/// Shared validation helpers for immutable configuration groups.
/// </summary>
public static class ConfigGuard
{
    /// <summary>
    /// Ensures a positive capacity is within an inclusive hard limit.
    /// </summary>
    /// <param name="group">Group name for exception context.</param>
    /// <param name="field">Field name.</param>
    /// <param name="value">Candidate value.</param>
    /// <param name="hardLimit">Inclusive maximum.</param>
    /// <param name="reasonCode">Stable failure code.</param>
    /// <returns>Validated value.</returns>
    public static int RequirePositiveAtMost(
        string group,
        string field,
        int value,
        int hardLimit,
        int reasonCode)
    {
        if (value < 1 || value > hardLimit)
        {
            throw new ConfigValidationException(
                group,
                field + " must be in 1.." + hardLimit.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".",
                reasonCode);
        }

        return value;
    }

    /// <summary>
    /// Ensures a non-negative value is within an inclusive hard limit.
    /// </summary>
    public static int RequireNonNegativeAtMost(
        string group,
        string field,
        int value,
        int hardLimit,
        int reasonCode)
    {
        if (value < 0 || value > hardLimit)
        {
            throw new ConfigValidationException(
                group,
                field + " must be in 0.." + hardLimit.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".",
                reasonCode);
        }

        return value;
    }

    /// <summary>
    /// Ensures tick delta is within <see cref="AiHardLimits.MaximumTickDeltaMilliseconds"/>.
    /// </summary>
    public static int RequireTickDelta(string group, int value, int reasonCode)
    {
        return RequirePositiveAtMost(
            group,
            "TickDeltaMilliseconds",
            value,
            AiHardLimits.MaximumTickDeltaMilliseconds,
            reasonCode);
    }

    /// <summary>
    /// Maps a validation exception into a structured result.
    /// </summary>
    public static ConfigurationValidationResult ToResult(ConfigValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return ConfigurationValidationResult.Failure(exception.ReasonCode, exception.ContextId);
    }
}
