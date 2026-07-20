using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Host interaction authorization and lifecycle for smart objects.
/// </summary>
/// <remarks>
/// Interaction begin/end calls are requests; the host retains authority over
/// animation and world mutation. Implementations must not allocate on the frozen
/// runtime path and must return statuses instead of throwing for expected failures.
/// </remarks>
public interface IInteractionService
{
    /// <summary>
    /// Returns whether the agent may begin interacting with a smart object.
    /// </summary>
    /// <param name="agent">Agent attempting interaction.</param>
    /// <param name="smartObject">Target smart object.</param>
    /// <returns>Success when authorized; otherwise a rejection status.</returns>
    public OperationStatus CanInteract(AgentId agent, SmartObjectId smartObject);

    /// <summary>
    /// Requests that an interaction begin.
    /// </summary>
    /// <param name="agent">Agent beginning interaction.</param>
    /// <param name="smartObject">Target smart object.</param>
    /// <returns>Success, in-progress, or rejection status.</returns>
    public OperationStatus BeginInteract(AgentId agent, SmartObjectId smartObject);

    /// <summary>
    /// Requests that an interaction end.
    /// </summary>
    /// <param name="agent">Agent ending interaction.</param>
    /// <param name="smartObject">Target smart object.</param>
    /// <returns>Success or rejection status.</returns>
    public OperationStatus EndInteract(AgentId agent, SmartObjectId smartObject);
}
