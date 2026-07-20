using TacticalGoap.Abstractions.Enums;

namespace TacticalGoap.Abstractions.Results;

/// <summary>
/// Explicit per-tick action executor result.
/// </summary>
/// <remarks>
/// The executor returns this value after each action tick. Callers must inspect
/// <see cref="Status"/> before interpreting <see cref="FailureReason"/>. Successful
/// and in-progress statuses leave <see cref="FailureReason"/> as
/// <see cref="ActionFailureReason.None"/>.
/// </remarks>
public readonly struct ActionTickResult
{
    /// <summary>
    /// Initializes a new action tick result.
    /// </summary>
    /// <param name="status">Action lifecycle status after the tick.</param>
    /// <param name="failureReason">Failure reason when status is failed or timed out.</param>
    /// <param name="ticksElapsed">Total ticks spent in the action so far.</param>
    public ActionTickResult(ActionStatus status, ActionFailureReason failureReason, int ticksElapsed)
    {
        Status = status;
        FailureReason = failureReason;
        TicksElapsed = ticksElapsed;
    }

    /// <summary>
    /// Gets the action lifecycle status after the tick.
    /// </summary>
    public ActionStatus Status { get; }

    /// <summary>
    /// Gets the failure reason when the action failed or timed out.
    /// </summary>
    public ActionFailureReason FailureReason { get; }

    /// <summary>
    /// Gets the total ticks elapsed while executing the action.
    /// </summary>
    public int TicksElapsed { get; }

    /// <summary>
    /// Gets a value indicating whether the action completed successfully.
    /// </summary>
    public bool IsSucceeded => Status == ActionStatus.Succeeded;

    /// <summary>
    /// Gets a value indicating whether the action is still running.
    /// </summary>
    public bool IsRunning =>
        Status == ActionStatus.Starting || Status == ActionStatus.Running;

    /// <summary>
    /// Gets a value indicating whether the action ended in a terminal failure state.
    /// </summary>
    public bool IsFailed =>
        Status == ActionStatus.Failed
        || Status == ActionStatus.Cancelled
        || Status == ActionStatus.TimedOut;

    /// <summary>
    /// Creates a running action tick result.
    /// </summary>
    /// <param name="ticksElapsed">Ticks spent so far.</param>
    /// <returns>A running result with no failure reason.</returns>
    public static ActionTickResult Running(int ticksElapsed)
    {
        return new ActionTickResult(ActionStatus.Running, ActionFailureReason.None, ticksElapsed);
    }

    /// <summary>
    /// Creates a successful action tick result.
    /// </summary>
    /// <param name="ticksElapsed">Ticks spent before success.</param>
    /// <returns>A succeeded result with no failure reason.</returns>
    public static ActionTickResult Succeeded(int ticksElapsed)
    {
        return new ActionTickResult(ActionStatus.Succeeded, ActionFailureReason.None, ticksElapsed);
    }

    /// <summary>
    /// Creates a failed action tick result.
    /// </summary>
    /// <param name="failureReason">Explicit failure reason.</param>
    /// <param name="ticksElapsed">Ticks spent before failure.</param>
    /// <returns>A failed result.</returns>
    public static ActionTickResult Failed(ActionFailureReason failureReason, int ticksElapsed)
    {
        return new ActionTickResult(ActionStatus.Failed, failureReason, ticksElapsed);
    }
}
