using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Bounded host command sink for agent locomotion and combat requests.
/// </summary>
/// <remarks>
/// Commands are advisory requests to the host. Implementations must reject work
/// that would allocate unbounded buffers or exceed per-tick capacity. Return
/// statuses instead of throwing for expected host rejections. The runtime owns
/// no command buffers across ticks; each call is self-contained.
/// </remarks>
public interface IAgentCommandSink
{
    /// <summary>
    /// Requests movement toward a destination cell.
    /// </summary>
    /// <param name="agent">Agent issuing the move.</param>
    /// <param name="destination">Target cell.</param>
    /// <returns>Operation status describing acceptance or rejection.</returns>
    public OperationStatus SubmitMove(AgentId agent, Int2 destination);

    /// <summary>
    /// Requests aiming or facing toward a direction.
    /// </summary>
    /// <param name="agent">Agent issuing the aim.</param>
    /// <param name="facing">Desired facing.</param>
    /// <returns>Operation status describing acceptance or rejection.</returns>
    public OperationStatus SubmitAim(AgentId agent, Direction8 facing);

    /// <summary>
    /// Requests firing the selected weapon at a target entity.
    /// </summary>
    /// <param name="agent">Agent issuing the fire request.</param>
    /// <param name="weapon">Weapon to fire.</param>
    /// <param name="target">Target entity.</param>
    /// <returns>Operation status describing acceptance or rejection.</returns>
    public OperationStatus SubmitFire(AgentId agent, WeaponId weapon, EntityId target);

    /// <summary>
    /// Requests a reload of the specified weapon.
    /// </summary>
    /// <param name="agent">Agent issuing the reload.</param>
    /// <param name="weapon">Weapon to reload.</param>
    /// <returns>Operation status describing acceptance or rejection.</returns>
    public OperationStatus SubmitReload(AgentId agent, WeaponId weapon);

    /// <summary>
    /// Requests interaction with a smart object.
    /// </summary>
    /// <param name="agent">Agent issuing the interaction.</param>
    /// <param name="smartObject">Smart object to interact with.</param>
    /// <returns>Operation status describing acceptance or rejection.</returns>
    public OperationStatus SubmitInteract(AgentId agent, SmartObjectId smartObject);

    /// <summary>
    /// Requests a host animation by stable animation code.
    /// </summary>
    /// <param name="agent">Agent issuing the animation.</param>
    /// <param name="animationCode">Host-defined animation identifier.</param>
    /// <returns>Operation status describing acceptance or rejection.</returns>
    public OperationStatus SubmitAnimate(AgentId agent, int animationCode);
}
