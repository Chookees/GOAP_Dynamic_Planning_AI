using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Configuration.Validation;

namespace TacticalGoap.Configuration.Groups;

/// <summary>
/// Immutable squad coordination configuration.
/// </summary>
public sealed class SquadConfiguration
{
    /// <summary>
    /// Creates a validated squad configuration.
    /// </summary>
    public static SquadConfiguration Create(
        int maxSquads,
        int maxAgentsPerSquad,
        int maxOrders,
        int maxSearchSectors,
        int reclusterIntervalTicks,
        int defaultOrderDurationTicks)
    {
        return new SquadConfiguration(
            ConfigGuard.RequirePositiveAtMost(nameof(SquadConfiguration), nameof(MaxSquads), maxSquads, AiHardLimits.MaximumSquads, 1801),
            ConfigGuard.RequirePositiveAtMost(nameof(SquadConfiguration), nameof(MaxAgentsPerSquad), maxAgentsPerSquad, AiHardLimits.MaximumAgentsPerSquad, 1802),
            ConfigGuard.RequirePositiveAtMost(nameof(SquadConfiguration), nameof(MaxOrders), maxOrders, AiHardLimits.MaximumSquadOrders, 1803),
            ConfigGuard.RequirePositiveAtMost(nameof(SquadConfiguration), nameof(MaxSearchSectors), maxSearchSectors, AiHardLimits.MaximumSearchSectors, 1804),
            ConfigGuard.RequirePositiveAtMost(nameof(SquadConfiguration), nameof(ReclusterIntervalTicks), reclusterIntervalTicks, 10_000, 1805),
            ConfigGuard.RequirePositiveAtMost(nameof(SquadConfiguration), nameof(DefaultOrderDurationTicks), defaultOrderDurationTicks, 10_000, 1806));
    }

    private SquadConfiguration(
        int maxSquads,
        int maxAgentsPerSquad,
        int maxOrders,
        int maxSearchSectors,
        int reclusterIntervalTicks,
        int defaultOrderDurationTicks)
    {
        MaxSquads = maxSquads;
        MaxAgentsPerSquad = maxAgentsPerSquad;
        MaxOrders = maxOrders;
        MaxSearchSectors = maxSearchSectors;
        ReclusterIntervalTicks = reclusterIntervalTicks;
        DefaultOrderDurationTicks = defaultOrderDurationTicks;
    }

    /// <summary>Gets the maximum concurrent squads.</summary>
    public int MaxSquads { get; }

    /// <summary>Gets the maximum agents per squad.</summary>
    public int MaxAgentsPerSquad { get; }

    /// <summary>Gets the maximum outstanding orders.</summary>
    public int MaxOrders { get; }

    /// <summary>Gets the maximum search sectors.</summary>
    public int MaxSearchSectors { get; }

    /// <summary>Gets the recluster interval in ticks.</summary>
    public int ReclusterIntervalTicks { get; }

    /// <summary>Gets the default order lifetime in ticks.</summary>
    public int DefaultOrderDurationTicks { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static SquadConfiguration CreateDefault() =>
        Create(
            AiHardLimits.MaximumSquads,
            AiHardLimits.MaximumAgentsPerSquad,
            AiHardLimits.MaximumSquadOrders,
            AiHardLimits.MaximumSearchSectors,
            30,
            45);
}
