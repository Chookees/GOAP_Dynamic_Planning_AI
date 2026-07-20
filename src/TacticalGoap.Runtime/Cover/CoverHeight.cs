namespace TacticalGoap.Runtime.Cover;

/// <summary>
/// Discrete cover height classification for tactical points.
/// </summary>
public enum CoverHeight : byte
{
    /// <summary>
    /// No cover height (open position).
    /// </summary>
    None = 0,

    /// <summary>
    /// Low cover usable while crouched or prone.
    /// </summary>
    Low = 1,

    /// <summary>
    /// High cover usable while standing or crouched.
    /// </summary>
    High = 2,
}
