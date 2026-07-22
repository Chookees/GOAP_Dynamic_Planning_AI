using System;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Switches to a bound weapon.
/// </summary>
public sealed class SwitchWeaponAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 2;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public SwitchWeaponAction()
        : base(WellKnownActionIds.SwitchWeapon, 1)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.SwitchWeapon,
            BaseCost,
            null,
            0,
            WorldFactId.WeaponSelected,
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
        if (!context.Candidate.BoundWeaponId.IsValid)
        {
            return ActionFailureReason.WeaponRejected;
        }

        if (context.Animation is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        OperationStatus animate = context.Animation.Play(context.AgentId, AnimationCodes.SwitchWeapon);
        return MapHostStatus(animate, ActionFailureReason.AnimationRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
