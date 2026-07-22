using System;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Opens a known door via host interaction.
/// </summary>
public sealed class OpenDoorAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 3;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public OpenDoorAction()
        : base(WellKnownActionIds.OpenDoor, 2)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.OpenDoor,
            BaseCost,
            WorldFactId.DoorKnown,
            1,
            WorldFactId.DoorOpen,
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

        if (context.Interaction is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        SmartObjectId door = SmartObjectId.FromInt32(context.Candidate.BoundEntityId.Value);
        OperationStatus can = context.Interaction.CanInteract(context.AgentId, door);
        if (can == OperationStatus.HostRejected || can == OperationStatus.Failed)
        {
            WriteDoorBlocked(ref context);
            return ActionFailureReason.DoorBlocked;
        }

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
        if (context.SmartObjects is not null && !context.SmartObjects.IsAvailable(door))
        {
            WriteDoorBlocked(ref context);
            return ActionFailureReason.DoorBlocked;
        }

        OperationStatus beginInteract = context.Interaction.BeginInteract(context.AgentId, door);
        if (beginInteract == OperationStatus.HostRejected || beginInteract == OperationStatus.Failed)
        {
            WriteDoorBlocked(ref context);
            return ActionFailureReason.DoorBlocked;
        }

        ActionFailureReason mapped = MapHostStatus(beginInteract, ActionFailureReason.InteractionRejected);
        if (mapped != ActionFailureReason.None)
        {
            return mapped;
        }

        AdvanceProgress(ref context);
        if (context.ProgressCounter >= context.RequiredProgressTicks && context.Interaction is not null)
        {
            _ = context.Interaction.EndInteract(context.AgentId, door);
        }

        return ActionFailureReason.None;
    }

    private static void WriteDoorBlocked(ref ActionExecutionContext context)
    {
        if (context.FailureKnowledge is null)
        {
            return;
        }

        _ = context.FailureKnowledge.TryWrite(
            context.AgentId,
            MemoryType.DoorBlocked,
            context.Candidate.BoundEntityId.Value,
            0,
            context.TickSequence);
    }
}
