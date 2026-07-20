using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Host animation playback requests and status queries.
/// </summary>
/// <remarks>
/// Animation codes are host-defined integers registered during configuration.
/// Implementations must not allocate on the frozen runtime path. Playback is
/// advisory; hosts may reject requests that conflict with higher-priority motion.
/// </remarks>
public interface IAnimationService
{
    /// <summary>
    /// Requests playback of an animation for an agent.
    /// </summary>
    /// <param name="agent">Agent to animate.</param>
    /// <param name="animationCode">Host-defined animation identifier.</param>
    /// <returns>Success or host rejection.</returns>
    public OperationStatus Play(AgentId agent, int animationCode);

    /// <summary>
    /// Attempts to determine whether an animation is currently playing.
    /// </summary>
    /// <param name="agent">Agent to query.</param>
    /// <param name="animationCode">Animation identifier to test.</param>
    /// <param name="playing">Receives whether the animation is playing.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryIsPlaying(AgentId agent, int animationCode, out bool playing);

    /// <summary>
    /// Requests that the current animation stop.
    /// </summary>
    /// <param name="agent">Agent to stop animating.</param>
    /// <returns>Success, no-op, or host rejection.</returns>
    public OperationStatus StopAnimation(AgentId agent);
}
