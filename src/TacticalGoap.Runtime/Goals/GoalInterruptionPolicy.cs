namespace TacticalGoap.Runtime.Goals;

/// <summary>
/// Policy controlling whether an active goal may be interrupted by arbitration.
/// </summary>
public enum GoalInterruptionPolicy : byte
{
    /// <summary>
    /// The active goal never yields until completed or failed.
    /// </summary>
    Never = 0,

    /// <summary>
    /// Only a strictly higher-priority relevant goal may interrupt.
    /// </summary>
    HigherPriorityOnly = 1,

    /// <summary>
    /// Any newly selected higher-scoring goal may interrupt.
    /// </summary>
    Always = 2,
}
