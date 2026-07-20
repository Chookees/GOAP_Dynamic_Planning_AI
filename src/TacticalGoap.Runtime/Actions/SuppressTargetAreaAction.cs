using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Suppresses a target area with sustained fire.
/// </summary>
public sealed class SuppressTargetAreaAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 3;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public SuppressTargetAreaAction()
        : base(WellKnownActionIds.SuppressTargetArea, 3)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.SuppressTargetArea,
            BaseCost,
            WorldFactId.HasAmmunition,
            1,
            WorldFactId.Suppressing,
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
        if (context.Commands is null || context.Weapons is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        WeaponId weapon = context.Candidate.BoundWeaponId;
        if (!weapon.IsValid)
        {
            OperationStatus selected = context.Weapons.TryGetSelectedWeapon(context.AgentId, out weapon);
            if (selected != OperationStatus.Success)
            {
                return ActionFailureReason.WeaponRejected;
            }
        }

        EntityId target = context.Candidate.BoundEntityId.IsValid
            ? context.Candidate.BoundEntityId
            : EntityId.FromInt32(0);
        OperationStatus canFire = context.Weapons.CanFire(context.AgentId, weapon, target);
        return MapHostStatus(canFire, ActionFailureReason.WeaponRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (context.Commands is null || context.Weapons is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        WeaponId weapon = context.Candidate.BoundWeaponId;
        if (!weapon.IsValid)
        {
            OperationStatus selected = context.Weapons.TryGetSelectedWeapon(context.AgentId, out weapon);
            if (selected != OperationStatus.Success)
            {
                return ActionFailureReason.WeaponRejected;
            }
        }

        EntityId target = context.Candidate.BoundEntityId.IsValid
            ? context.Candidate.BoundEntityId
            : EntityId.FromInt32(0);
        OperationStatus fire = context.Commands.SubmitFire(context.AgentId, weapon, target);
        ActionFailureReason mapped = MapHostStatus(fire, ActionFailureReason.WeaponRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
