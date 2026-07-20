using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Marks the active squad order satisfied.
/// </summary>
public sealed class CompleteSquadOrderAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 1;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public CompleteSquadOrderAction()
        : base(WellKnownActionIds.CompleteSquadOrder, 1)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.CompleteSquadOrder,
            BaseCost,
            WorldFactId.SquadOrderAvailable,
            1,
            WorldFactId.SquadOrderSatisfied,
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
        if (!context.FocusOrder.IsValid)
        {
            return 0;
        }

        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            EntityId.Invalid,
            context.FocusPoint,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            context.FocusOrder,
            SearchSectorId.Invalid);
        return writer.TryAdd(candidate) ? 1 : 0;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
    {
        if (context.Animation is not null)
        {
            OperationStatus animate = context.Animation.Play(context.AgentId, AnimationCodes.Idle);
            ActionFailureReason mapped = MapHostStatus(animate, ActionFailureReason.AnimationRejected);
            if (mapped != ActionFailureReason.None)
            {
                return mapped;
            }
        }

        return ActionFailureReason.None;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
