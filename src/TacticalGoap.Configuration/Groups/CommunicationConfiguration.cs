using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Configuration.Validation;

namespace TacticalGoap.Configuration.Groups;

/// <summary>
/// Immutable communication capacity configuration.
/// </summary>
public sealed class CommunicationConfiguration
{
    /// <summary>
    /// Creates a validated communication configuration.
    /// </summary>
    public static CommunicationConfiguration Create(int maxPendingRequests)
    {
        return new CommunicationConfiguration(
            ConfigGuard.RequirePositiveAtMost(
                nameof(CommunicationConfiguration),
                nameof(MaxPendingRequests),
                maxPendingRequests,
                AiHardLimits.MaximumCommunicationRequests,
                1901));
    }

    private CommunicationConfiguration(int maxPendingRequests)
    {
        MaxPendingRequests = maxPendingRequests;
    }

    /// <summary>Gets the maximum pending communication requests.</summary>
    public int MaxPendingRequests { get; }

    /// <summary>Creates the sample default configuration.</summary>
    public static CommunicationConfiguration CreateDefault() =>
        Create(AiHardLimits.MaximumCommunicationRequests);
}
