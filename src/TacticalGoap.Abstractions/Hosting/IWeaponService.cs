using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Hosting;

/// <summary>
/// Host weapon state and fire-authorization queries.
/// </summary>
/// <remarks>
/// Implementations must expose deterministic reads for identical agent and weapon
/// state within a tick. Methods must not allocate on the frozen runtime path.
/// Fire authorization does not itself discharge a weapon; use
/// <see cref="IAgentCommandSink.SubmitFire"/> to request firing.
/// </remarks>
public interface IWeaponService
{
    /// <summary>
    /// Attempts to read the currently selected weapon for an agent.
    /// </summary>
    /// <param name="agent">Agent to query.</param>
    /// <param name="weapon">Receives the selected weapon when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetSelectedWeapon(AgentId agent, out WeaponId weapon);

    /// <summary>
    /// Attempts to read remaining ammunition for a weapon.
    /// </summary>
    /// <param name="agent">Agent owning the weapon.</param>
    /// <param name="weapon">Weapon to query.</param>
    /// <param name="rounds">Receives remaining rounds when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetAmmunition(AgentId agent, WeaponId weapon, out int rounds);

    /// <summary>
    /// Returns whether the agent may fire the weapon at the target.
    /// </summary>
    /// <param name="agent">Agent attempting to fire.</param>
    /// <param name="weapon">Weapon to fire.</param>
    /// <param name="target">Target entity.</param>
    /// <returns>Success when authorized; otherwise a rejection status.</returns>
    public OperationStatus CanFire(AgentId agent, WeaponId weapon, EntityId target);

    /// <summary>
    /// Returns whether the weapon requires a reload.
    /// </summary>
    /// <param name="agent">Agent owning the weapon.</param>
    /// <param name="weapon">Weapon to query.</param>
    /// <param name="requiresReload">Receives whether reload is required.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetRequiresReload(AgentId agent, WeaponId weapon, out bool requiresReload);
}
