using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Configuration.Validation;

namespace DynamicPlanningAI.Configuration.Groups;

/// <summary>
/// Immutable navigation capacity configuration.
/// </summary>
public sealed class NavigationConfiguration
{
    /// <summary>
    /// Creates a validated navigation configuration.
    /// </summary>
    public static NavigationConfiguration Create(
        int maxPathNodes,
        int maxExpansions,
        int gridWidth,
        int gridHeight)
    {
        return new NavigationConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(NavigationConfiguration), nameof(MaxPathNodes), maxPathNodes, AiHardLimits.MaximumNavigationPathNodes, 1701),
            ConfigGuard.RequirePositiveAtMost(nameof(NavigationConfiguration), nameof(MaxExpansions), maxExpansions, 4096, 1702),
            ConfigGuard.RequirePositiveAtMost(nameof(NavigationConfiguration), nameof(GridWidth), gridWidth, 128, 1703),
            ConfigGuard.RequirePositiveAtMost(nameof(NavigationConfiguration), nameof(GridHeight), gridHeight, 128, 1704));
    }

    private NavigationConfiguration(int maxPathNodes, int maxExpansions, int gridWidth, int gridHeight)
    {
        MaxPathNodes = maxPathNodes;
        MaxExpansions = maxExpansions;
        GridWidth = gridWidth;
        GridHeight = gridHeight;
    }

    /// <summary>Gets the maximum path nodes written per query.</summary>
    public int MaxPathNodes { get; }

    /// <summary>Gets the maximum A*/BFS expansions per query.</summary>
    public int MaxExpansions { get; }

    /// <summary>Gets the sample grid width.</summary>
    public int GridWidth { get; }

    /// <summary>Gets the sample grid height.</summary>
    public int GridHeight { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static NavigationConfiguration CreateDefault() =>
        Create(AiHardLimits.MaximumNavigationPathNodes, 512, 24, 16);
}
