using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Attacks a visible living target.
/// </summary>
public sealed class AttackTargetAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 2;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public AttackTargetAction()
        : base(WellKnownActionIds.AttackTarget, 2)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.AttackTarget,
            BaseCost,
            WorldFactId.TargetVisible,
            1,
            WorldFactId.TargetAlive,
            0,
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
        if (!context.FocusEntity.IsValid)
        {
            return 0;
        }

        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            context.FocusEntity,
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
        if (!context.Candidate.BoundEntityId.IsValid)
        {
            return ActionFailureReason.TargetLost;
        }

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
        }

        OperationStatus canFire = context.Weapons.CanFire(context.AgentId, weapon, context.Candidate.BoundEntityId);
        return MapHostStatus(canFire, ActionFailureReason.WeaponRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (!context.Candidate.BoundEntityId.IsValid)
        {
            return ActionFailureReason.TargetLost;
        }

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
        }

        OperationStatus ammo = context.Weapons.TryGetAmmunition(context.AgentId, weapon, out int rounds);
        if (ammo == OperationStatus.Success && rounds <= 0)
        {
            return ActionFailureReason.NoAmmunition;
        }

        OperationStatus fire = context.Commands.SubmitFire(context.AgentId, weapon, context.Candidate.BoundEntityId);
        ActionFailureReason mapped = MapHostStatus(fire, ActionFailureReason.WeaponRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
