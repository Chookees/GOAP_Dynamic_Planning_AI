using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Configuration.Validation;

namespace TacticalGoap.Configuration.Groups;

/// <summary>
/// Immutable goal arbitration configuration.
/// </summary>
public sealed class GoalConfiguration
{
    /// <summary>
    /// Creates a validated goal configuration.
    /// </summary>
    public static GoalConfiguration Create(int maxGoalsPerAgent, int hysteresisMargin, int dangerPriorityFloor)
    {
        return new GoalConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(GoalConfiguration), nameof(MaxGoalsPerAgent), maxGoalsPerAgent, AiHardLimits.MaximumGoalsPerAgent, 1401),
            ConfigGuard.RequireNonNegativeAtMost(nameof(GoalConfiguration), nameof(HysteresisMargin), hysteresisMargin, 1000, 1402),
            ConfigGuard.RequirePositiveAtMost(nameof(GoalConfiguration), nameof(DangerPriorityFloor), dangerPriorityFloor, 10_000, 1403));
    }

    private GoalConfiguration(int maxGoalsPerAgent, int hysteresisMargin, int dangerPriorityFloor)
    {
        MaxGoalsPerAgent = maxGoalsPerAgent;
        HysteresisMargin = hysteresisMargin;
        DangerPriorityFloor = dangerPriorityFloor;
    }

    /// <summary>Gets the maximum goals registered per agent.</summary>
    public int MaxGoalsPerAgent { get; }

    /// <summary>Gets the arbitration hysteresis margin.</summary>
    public int HysteresisMargin { get; }

    /// <summary>Gets the minimum priority for immediate danger goals.</summary>
    public int DangerPriorityFloor { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static GoalConfiguration CreateDefault() => Create(16, 10, 9000);
}
