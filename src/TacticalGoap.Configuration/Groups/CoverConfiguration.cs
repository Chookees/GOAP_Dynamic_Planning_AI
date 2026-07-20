using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Configuration.Validation;

namespace TacticalGoap.Configuration.Groups;

/// <summary>
/// Immutable cover scoring weight configuration.
/// </summary>
public sealed class CoverConfiguration
{
    /// <summary>
    /// Creates a validated cover configuration.
    /// </summary>
    public static CoverConfiguration Create(
        int maxCandidates,
        int maxTacticalPoints,
        int flankAngleWeight,
        int pathCostWeight,
        int qualityWeight)
    {
        return new CoverConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(CoverConfiguration), nameof(MaxCandidates), maxCandidates, AiHardLimits.MaximumCoverCandidates, 1601),
            ConfigGuard.RequirePositiveAtMost(nameof(CoverConfiguration), nameof(MaxTacticalPoints), maxTacticalPoints, AiHardLimits.MaximumTacticalPoints, 1602),
            ConfigGuard.RequireNonNegativeAtMost(nameof(CoverConfiguration), nameof(FlankAngleWeight), flankAngleWeight, 10_000, 1603),
            ConfigGuard.RequireNonNegativeAtMost(nameof(CoverConfiguration), nameof(PathCostWeight), pathCostWeight, 10_000, 1604),
            ConfigGuard.RequireNonNegativeAtMost(nameof(CoverConfiguration), nameof(QualityWeight), qualityWeight, 10_000, 1605));
    }

    private CoverConfiguration(
        int maxCandidates,
        int maxTacticalPoints,
        int flankAngleWeight,
        int pathCostWeight,
        int qualityWeight)
    {
        MaxCandidates = maxCandidates;
        MaxTacticalPoints = maxTacticalPoints;
        FlankAngleWeight = flankAngleWeight;
        PathCostWeight = pathCostWeight;
        QualityWeight = qualityWeight;
    }

    /// <summary>Gets the maximum cover candidates scored per query.</summary>
    public int MaxCandidates { get; }

    /// <summary>Gets the maximum tactical points in a scenario.</summary>
    public int MaxTacticalPoints { get; }

    /// <summary>Gets the flank-angle scoring weight.</summary>
    public int FlankAngleWeight { get; }

    /// <summary>Gets the path-cost scoring weight.</summary>
    public int PathCostWeight { get; }

    /// <summary>Gets the quality scoring weight.</summary>
    public int QualityWeight { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static CoverConfiguration CreateDefault() =>
        Create(AiHardLimits.MaximumCoverCandidates, AiHardLimits.MaximumTacticalPoints, 40, 2, 10);
}
