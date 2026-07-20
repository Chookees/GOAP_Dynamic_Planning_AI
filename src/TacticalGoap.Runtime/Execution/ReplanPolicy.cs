using System;
using TacticalGoap.Abstractions.Limits;

namespace TacticalGoap.Runtime.Execution;

/// <summary>
/// Bounded replanning policy for plan execution failures and interrupts.
/// </summary>
public sealed class ReplanPolicy
{
    /// <summary>
    /// Initializes a policy with hard-limit defaults.
    /// </summary>
    public ReplanPolicy()
        : this(AiHardLimits.MaximumReplanningAttempts, minDelayTicks: 1, allowEmergencyInterrupt: true)
    {
    }

    /// <summary>
    /// Initializes a policy with explicit bounds.
    /// </summary>
    /// <param name="maxAttempts">Maximum replanning attempts per goal activation.</param>
    /// <param name="minDelayTicks">Minimum ticks between replan requests.</param>
    /// <param name="allowEmergencyInterrupt">Whether emergency interrupts bypass delay.</param>
    public ReplanPolicy(int maxAttempts, int minDelayTicks, bool allowEmergencyInterrupt)
    {
        if (maxAttempts < 0 || maxAttempts > AiHardLimits.MaximumReplanningAttempts)
        {
            throw new System.ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(minDelayTicks);

        MaxAttempts = maxAttempts;
        MinDelayTicks = minDelayTicks;
        AllowEmergencyInterrupt = allowEmergencyInterrupt;
        AttemptsUsed = 0;
        LastReplanTick = -1L;
    }

    /// <summary>Gets the maximum replanning attempts.</summary>
    public int MaxAttempts { get; }

    /// <summary>Gets the minimum delay between replans.</summary>
    public int MinDelayTicks { get; }

    /// <summary>Gets whether emergency interrupts may bypass delay.</summary>
    public bool AllowEmergencyInterrupt { get; }

    /// <summary>Gets attempts used for the current goal activation.</summary>
    public int AttemptsUsed { get; private set; }

    /// <summary>Gets the tick of the last accepted replan.</summary>
    public long LastReplanTick { get; private set; }

    /// <summary>
    /// Returns whether a replan may be requested at the given tick.
    /// </summary>
    /// <param name="tickSequence">Current tick.</param>
    /// <param name="emergency">Whether the request is an emergency interrupt.</param>
    /// <returns><see langword="true"/> when a replan is permitted.</returns>
    public bool CanReplan(long tickSequence, bool emergency)
    {
        if (AttemptsUsed >= MaxAttempts)
        {
            return false;
        }

        if (LastReplanTick < 0L)
        {
            return true;
        }

        long elapsed = checked(tickSequence - LastReplanTick);
        if (elapsed >= MinDelayTicks)
        {
            return true;
        }

        return emergency && AllowEmergencyInterrupt;
    }

    /// <summary>
    /// Records that a replan was accepted.
    /// </summary>
    /// <param name="tickSequence">Tick of the replan.</param>
    public void RecordReplan(long tickSequence)
    {
        AttemptsUsed = checked(AttemptsUsed + 1);
        LastReplanTick = tickSequence;
    }

    /// <summary>
    /// Resets attempt counters for a new goal activation.
    /// </summary>
    public void Reset()
    {
        AttemptsUsed = 0;
        LastReplanTick = -1L;
    }
}
