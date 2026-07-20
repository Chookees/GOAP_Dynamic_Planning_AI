namespace TacticalGoap.Abstractions.Geometry;

/// <summary>
/// Discrete distance category used after quantizing continuous ranges.
/// </summary>
/// <remarks>
/// Thresholds are configured during initialization. Planning and scoring must
/// consume these categories rather than floating-point distances.
/// </remarks>
public enum DistanceCategory : byte
{
    /// <summary>
    /// Within the near engagement band.
    /// </summary>
    Near = 0,

    /// <summary>
    /// Within the medium engagement band.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Within the far engagement band.
    /// </summary>
    Far = 2,

    /// <summary>
    /// Beyond the configured maximum engagement range.
    /// </summary>
    OutOfRange = 3,
}
