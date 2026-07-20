using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Throws a grenade toward a threat area.
/// </summary>
public sealed class ThrowGrenadeAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 4;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public ThrowGrenadeAction()
        : base(WellKnownActionIds.ThrowGrenade, 2)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.ThrowGrenade,
            BaseCost,
            WorldFactId.HasGrenade,
            1,
            WorldFactId.ImmediateDangerResolved,
            1,
            WorldFactId.HasGrenade);
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
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        return writer.TryAdd(candidate) ? 1 : 0;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
    {
        if (context.Commands is null || context.Animation is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        OperationStatus animate = context.Animation.Play(context.AgentId, AnimationCodes.ThrowGrenade);
        return MapHostStatus(animate, ActionFailureReason.AnimationRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
