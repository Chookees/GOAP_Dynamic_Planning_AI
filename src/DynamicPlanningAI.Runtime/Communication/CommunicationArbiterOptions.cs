using DynamicPlanningAI.Abstractions.Limits;

namespace DynamicPlanningAI.Runtime.Communication;

/// <summary>
/// Configuration for <see cref="CommunicationArbiter"/>.
/// </summary>
public sealed class CommunicationArbiterOptions
{
    /// <summary>
    /// Initializes default arbitration options.
    /// </summary>
    public CommunicationArbiterOptions()
    {
        MaxPendingRequests = AiHardLimits.MaximumCommunicationRequests;
        MaxConcurrentEmissions = 4;
        SpeakerCooldownTicks = 3;
        SquadChannelCooldownTicks = 2;
        DuplicateSuppressionTicks = 5;
        DefaultTimeToLiveTicks = 16;
    }

    /// <summary>Gets or sets the pending request capacity (≤ hard limit).</summary>
    public int MaxPendingRequests { get; set; }

    /// <summary>Gets or sets the maximum emissions per tick.</summary>
    public int MaxConcurrentEmissions { get; set; }

    /// <summary>Gets or sets speaker cooldown in ticks.</summary>
    public int SpeakerCooldownTicks { get; set; }

    /// <summary>Gets or sets squad channel cooldown in ticks.</summary>
    public int SquadChannelCooldownTicks { get; set; }

    /// <summary>Gets or sets duplicate suppression window in ticks.</summary>
    public int DuplicateSuppressionTicks { get; set; }

    /// <summary>Gets or sets default request TTL when expiry is omitted.</summary>
    public int DefaultTimeToLiveTicks { get; set; }

    /// <summary>
    /// Validates options against hard limits.
    /// </summary>
    /// <returns><see langword="true"/> when valid.</returns>
    public bool Validate()
    {
        if (MaxPendingRequests < 1 || MaxPendingRequests > AiHardLimits.MaximumCommunicationRequests)
        {
            return false;
        }

        if (MaxConcurrentEmissions < 1 || MaxConcurrentEmissions > AiHardLimits.MaximumCommunicationRequests)
        {
            return false;
        }

        if (SpeakerCooldownTicks < 0 || SquadChannelCooldownTicks < 0 || DuplicateSuppressionTicks < 0)
        {
            return false;
        }

        if (DefaultTimeToLiveTicks < 1)
        {
            return false;
        }

        return true;
    }
}
