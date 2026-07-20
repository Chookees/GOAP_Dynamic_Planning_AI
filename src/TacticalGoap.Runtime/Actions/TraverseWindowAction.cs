using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Traverses a window interaction point.
/// </summary>
public sealed class TraverseWindowAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 4;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public TraverseWindowAction()
        : base(WellKnownActionIds.TraverseWindow, 2)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.TraverseWindow,
            BaseCost,
            WorldFactId.AtWindowTraversal,
            1,
            WorldFactId.WindowTraversed,
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
        TacticalPointId point = context.FocusPoint;
        if (!point.IsValid)
        {
            return 0;
        }

        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            EntityId.Invalid,
            point,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            SearchSectorId.Invalid);
        return writer.TryAdd(candidate) ? 1 : 0;
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnBegin(ref ActionExecutionContext context)
    {
        if (context.Animation is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        OperationStatus animate = context.Animation.Play(context.AgentId, AnimationCodes.Traverse);
        return MapHostStatus(animate, ActionFailureReason.AnimationRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (context.Animation is null)
        {
            AdvanceProgress(ref context);
            return ActionFailureReason.None;
        }

        OperationStatus playing = context.Animation.TryIsPlaying(context.AgentId, AnimationCodes.Traverse, out bool isPlaying);
        if (playing == OperationStatus.HostRejected)
        {
            return ActionFailureReason.TraversalUnavailable;
        }

        ActionFailureReason mapped = MapHostStatus(playing, ActionFailureReason.AnimationRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        if (!isPlaying && context.ProgressCounter > 0)
        {
            context.ProgressCounter = context.RequiredProgressTicks;
            return ActionFailureReason.None;
        }

        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
