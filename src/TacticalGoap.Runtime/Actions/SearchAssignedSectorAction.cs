using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Searches a squad-assigned sector.
/// </summary>
public sealed class SearchAssignedSectorAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 4;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public SearchAssignedSectorAction()
        : base(WellKnownActionIds.SearchAssignedSector, 3)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.SearchAssignedSector,
            BaseCost,
            WorldFactId.SquadOrderAvailable,
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
        SearchSectorId sector = context.FocusSector;
        if (!sector.IsValid)
        {
            return 0;
        }

        ActionCandidate candidate = ActionFactory.CreateCandidate(
            definition,
            EntityId.Invalid,
            TacticalPointId.Invalid,
            NavigationNodeId.Invalid,
            WeaponId.Invalid,
            OrderId.Invalid,
            sector);
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
