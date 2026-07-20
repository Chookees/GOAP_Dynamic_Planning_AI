using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Breaches a blocked door.
/// </summary>
public sealed class BreachDoorAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 8;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public BreachDoorAction()
        : base(WellKnownActionIds.BreachDoor, 4)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.BreachDoor,
            BaseCost,
            WorldFactId.DoorBlocked,
            1,
            WorldFactId.DoorOpen,
            1,
            WorldFactId.DoorBlocked);
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
        if (!context.Candidate.BoundEntityId.IsValid)
        {
            return ActionFailureReason.InvalidPrecondition;
        }

        if (context.Interaction is null || context.Animation is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        SmartObjectId door = SmartObjectId.FromInt32(context.Candidate.BoundEntityId.Value);
        OperationStatus animate = context.Animation.Play(context.AgentId, AnimationCodes.Breach);
        ActionFailureReason mapped = MapHostStatus(animate, ActionFailureReason.AnimationRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        OperationStatus can = context.Interaction.CanInteract(context.AgentId, door);
        return MapHostStatus(can, ActionFailureReason.InteractionRejected);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        if (!context.Candidate.BoundEntityId.IsValid)
        {
            return ActionFailureReason.InvalidPrecondition;
        }

        if (context.Interaction is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        SmartObjectId door = SmartObjectId.FromInt32(context.Candidate.BoundEntityId.Value);
        OperationStatus interact = context.Interaction.BeginInteract(context.AgentId, door);
        ActionFailureReason mapped = MapHostStatus(interact, ActionFailureReason.InteractionRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        AdvanceProgress(ref context);
        if (context.ProgressCounter >= context.RequiredProgressTicks && context.Interaction is not null)
        {
            _ = context.Interaction.EndInteract(context.AgentId, door);
            if (context.FailureKnowledge is not null)
            {
                _ = context.FailureKnowledge.TryWrite(
                    context.AgentId,
                    MemoryType.DoorBreached,
                    context.Candidate.BoundEntityId.Value,
                    0,
                    context.TickSequence);
            }
        }

        return ActionFailureReason.None;
    }
}
