using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Holds aim on a visible target.
/// </summary>
public sealed class HoldAimAction : ActionExecutorBase, IActionCandidateGenerator
{
    private const int BaseCost = 1;

    /// <summary>
    /// Initializes the action executor.
    /// </summary>
    public HoldAimAction()
        : base(WellKnownActionIds.HoldAim, 2)
    {
    }

    /// <summary>
    /// Creates the immutable planning definition for this action.
    /// </summary>
    /// <returns>Action definition template.</returns>
    public static ActionDefinition CreateDefinition()
    {
        return ActionFactory.CreateDefinition(
            WellKnownActionIds.HoldAim,
            BaseCost,
            WorldFactId.TargetVisible,
            1,
            WorldFactId.TargetVisible,
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
        if (context.Commands is null)
        {
            return ActionFailureReason.HostServiceFailure;
        }

        OperationStatus aim = context.Commands.SubmitAim(context.AgentId, Direction8.North);
        return MapHostStatus(aim, ActionFailureReason.HostServiceFailure);
    }

    /// <inheritdoc />
    protected override ActionFailureReason OnTick(ref ActionExecutionContext context)
    {
        AdvanceProgress(ref context);
        return ActionFailureReason.None;
    }
}
