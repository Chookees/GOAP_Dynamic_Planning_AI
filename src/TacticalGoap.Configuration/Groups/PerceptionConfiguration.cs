using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Configuration.Validation;

namespace TacticalGoap.Configuration.Groups;

/// <summary>
/// Immutable perception capacity configuration.
/// </summary>
public sealed class PerceptionConfiguration
{
    /// <summary>
    /// Creates a validated perception configuration.
    /// </summary>
    public static PerceptionConfiguration Create(
        int maxCandidates,
        int maxSoundEvents,
        int maxDamageEvents,
        int visionRangeCells)
    {
        return new PerceptionConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(PerceptionConfiguration), nameof(MaxCandidates), maxCandidates, AiHardLimits.MaximumPerceptionCandidates, 1201),
            ConfigGuard.RequirePositiveAtMost(nameof(PerceptionConfiguration), nameof(MaxSoundEvents), maxSoundEvents, AiHardLimits.MaximumSoundEvents, 1202),
            ConfigGuard.RequirePositiveAtMost(nameof(PerceptionConfiguration), nameof(MaxDamageEvents), maxDamageEvents, AiHardLimits.MaximumDamageEvents, 1203),
            ConfigGuard.RequirePositiveAtMost(nameof(PerceptionConfiguration), nameof(VisionRangeCells), visionRangeCells, 64, 1204));
    }

    private PerceptionConfiguration(int maxCandidates, int maxSoundEvents, int maxDamageEvents, int visionRangeCells)
    {
        MaxCandidates = maxCandidates;
        MaxSoundEvents = maxSoundEvents;
        MaxDamageEvents = maxDamageEvents;
        VisionRangeCells = visionRangeCells;
    }

    /// <summary>Gets the maximum perception candidates per tick.</summary>
    public int MaxCandidates { get; }

    /// <summary>Gets the maximum sound events buffered per tick.</summary>
    public int MaxSoundEvents { get; }

    /// <summary>Gets the maximum damage events buffered per tick.</summary>
    public int MaxDamageEvents { get; }

    /// <summary>Gets the vision range in cells.</summary>
    public int VisionRangeCells { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static PerceptionConfiguration CreateDefault() =>
        Create(
            AiHardLimits.MaximumPerceptionCandidates,
            AiHardLimits.MaximumSoundEvents,
            AiHardLimits.MaximumDamageEvents,
            12);
}
