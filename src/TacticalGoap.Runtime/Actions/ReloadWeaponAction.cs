using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Reloads the selected weapon.
/// </summary>
public sealed class ReloadWeaponAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 2;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public ReloadWeaponAction()
        : base(WellKnownActionIds.ReloadWeapon, 2)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.ReloadWeapon,
            BaseCost,
            WorldFactId.HasAmmunition,
            1,
            WorldFactId.WeaponLoaded,
            1,
            null);
    }

    /// <summary>
    /// Generates a grounded candidate for this action when focus bindings allow.
    /// </summary>
    /// <inheritdoc />
    public int Generate(
        in CandidateGenerationContext context,
        ActionDefinition definition,
        BoundedCandidateWriter writer)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(writer);
        WeaponId weapon = context.FocusWeapon.IsValid ? context.FocusWeapon : WeaponId.FromInt32(0);
        if (!context.FocusWeapon.IsValid)
        {
            // Still emit a candidate; executor may resolve selected weapon at begin.
            weapon = WeaponId.Invalid;
        }

        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            EntityId.Invalid,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            context.FocusWeapon,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        return writer.TryAdd(candidate) ? 1 : 0;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
    {
        if (context.Commands is null || context.Weapons is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        WeaponId weapon = context.Candidate.BoundWeaponId;
        if (!weapon.IsValid)
        {
            OperationStatus selected = context.Weapons.TryGetSelectedWeapon(context.AgentId, out weapon);
            if (selected != OperationStatus.Success || !weapon.IsValid)
            {
                return ActionFailureReason.WeaponRejected;
            }

            context.Candidate = CloneWithWeapon(context.Candidate, weapon);
        }

        OperationStatus ammo = context.Weapons.TryGetAmmunition(context.AgentId, weapon, out int rounds);
        if (ammo == OperationStatus.Success && rounds <= 0)
        {
            return ActionFailureReason.NoAmmunition;
        }

        OperationStatus reload = context.Commands.SubmitReload(context.AgentId, weapon);
        return MapHostStatus(reload, ActionFailureReason.WeaponRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (context.Weapons is null)
        {
            AdvanceProgress(ref context);
            return ActionFailureReason.None;
        }

        WeaponId weapon = context.Candidate.BoundWeaponId;
        if (!weapon.IsValid)
        {
            return ActionFailureReason.WeaponRejected;
        }

        OperationStatus requires = context.Weapons.TryGetRequiresReload(context.AgentId, weapon, out bool needsReload);
        if (requires == OperationStatus.Success && !needsReload)
        {
            context.ProgressCounter = context.RequiredProgressTicks;
            return ActionFailureReason.None;
        }

        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }

    private static ActionCandidate CloneWithWeapon(ActionCandidate candidate, WeaponId weapon)
    {
        candidate.BoundWeaponId = weapon;
        return candidate;
    }
}
