using System;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Accepts an outstanding squad order.
/// </summary>
public sealed class AcceptSquadOrderAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 1;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public AcceptSquadOrderAction()
        : base(WellKnownActionIds.AcceptSquadOrder, 1)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.AcceptSquadOrder,
            BaseCost,
            WorldFactId.SquadOrderAvailable,
            1,
            WorldFactId.SquadOrderAvailable,
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
