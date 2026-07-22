using System;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Investigates a heard stimulus.
/// </summary>
public sealed class InvestigateSoundAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 4;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public InvestigateSoundAction()
        : base(WellKnownActionIds.InvestigateSound, 3)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.InvestigateSound,
            BaseCost,
            WorldFactId.TargetKnown,
            1,
            WorldFactId.SearchLocationInspected,
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
        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            context.FocusEntity,
            context.FocusPoint,
            context.FocusNode,
            context.FocusWeapon,
            context.FocusOrder,
            context.FocusSector);
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
