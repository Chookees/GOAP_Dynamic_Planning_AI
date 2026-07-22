using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Configuration.Validation;

namespace DynamicPlanningAI.Configuration.Groups;

/// <summary>
/// Immutable diagnostics ring-buffer configuration.
/// </summary>
public sealed class DiagnosticsConfiguration
{
    /// <summary>
    /// Creates a validated diagnostics configuration.
    /// </summary>
    public static DiagnosticsConfiguration Create(int ringBufferCapacity, bool enabled)
    {
        return new DiagnosticsConfiguration(
            ConfigGuard.RequirePositiveAtMost(
                nameof(DiagnosticsConfiguration),
                nameof(RingBufferCapacity),
                ringBufferCapacity,
                AiHardLimits.MaximumDiagnosticRecords,
                2001),
            enabled);
    }

    private DiagnosticsConfiguration(int ringBufferCapacity, bool enabled)
    {
        RingBufferCapacity = ringBufferCapacity;
        Enabled = enabled;
    }

    /// <summary>Gets the diagnostic ring-buffer capacity.</summary>
    public int RingBufferCapacity { get; }

    /// <summary>Gets a value indicating whether diagnostics are enabled.</summary>
    public bool Enabled { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static DiagnosticsConfiguration CreateDefault() =>
        Create(1024, enabled: true);
}
