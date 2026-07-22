using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Configuration.Validation;

namespace DynamicPlanningAI.Configuration.Groups;

/// <summary>
/// Immutable working-memory capacity configuration.
/// </summary>
public sealed class MemoryConfiguration
{
    /// <summary>
    /// Creates a validated memory configuration.
    /// </summary>
    public static MemoryConfiguration Create(int maxRecords, int defaultConfidence)
    {
        return new MemoryConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(MemoryConfiguration), nameof(MaxRecords), maxRecords, AiHardLimits.MaximumMemoryRecords, 1301),
            ConfigGuard.RequireNonNegativeAtMost(nameof(MemoryConfiguration), nameof(DefaultConfidence), defaultConfidence, 1000, 1302));
    }

    private MemoryConfiguration(int maxRecords, int defaultConfidence)
    {
        MaxRecords = maxRecords;
        DefaultConfidence = defaultConfidence;
    }

    /// <summary>Gets the maximum memory records per agent.</summary>
    public int MaxRecords { get; }

    /// <summary>Gets the default confidence for new evidence (0..1000).</summary>
    public int DefaultConfidence { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static MemoryConfiguration CreateDefault() =>
        Create(AiHardLimits.MaximumMemoryRecords, 800);
}
