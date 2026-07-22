using DynamicPlanningAI.Configuration.Validation;

namespace DynamicPlanningAI.Configuration.Groups;

/// <summary>
/// Immutable sample simulation configuration.
/// </summary>
public sealed class SampleConfiguration
{
    /// <summary>
    /// Creates a validated sample configuration.
    /// </summary>
    public static SampleConfiguration Create(
        int defaultMaxTicks,
        int defaultSeed,
        string defaultScenario,
        string traceDirectory)
    {
        if (string.IsNullOrWhiteSpace(defaultScenario))
        {
            throw new ConfigValidationException(
                nameof(SampleConfiguration),
                "DefaultScenario must be non-empty.",
                2101);
        }

        if (string.IsNullOrWhiteSpace(traceDirectory))
        {
            throw new ConfigValidationException(
                nameof(SampleConfiguration),
                "TraceDirectory must be non-empty.",
                2102);
        }

        return new SampleConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(SampleConfiguration), nameof(DefaultMaxTicks), defaultMaxTicks, 100_000, 2103),
            defaultSeed,
            defaultScenario.Trim(),
            traceDirectory.Trim());
    }

    private SampleConfiguration(int defaultMaxTicks, int defaultSeed, string defaultScenario, string traceDirectory)
    {
        DefaultMaxTicks = defaultMaxTicks;
        DefaultSeed = defaultSeed;
        DefaultScenario = defaultScenario;
        TraceDirectory = traceDirectory;
    }

    /// <summary>Gets the default maximum simulation ticks.</summary>
    public int DefaultMaxTicks { get; }

    /// <summary>Gets the default deterministic seed.</summary>
    public int DefaultSeed { get; }

    /// <summary>Gets the default scenario name.</summary>
    public string DefaultScenario { get; }

    /// <summary>Gets the directory used for trace files.</summary>
    public string TraceDirectory { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static SampleConfiguration CreateDefault() =>
        Create(256, 42, "BasicAttack", "traces");
}
