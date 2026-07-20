using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Helpers for building action definitions and grounded candidates.
/// </summary>
public static class ActionFactory
{
    /// <summary>
    /// Creates a definition with optional single precondition and effect facts.
    /// </summary>
    /// <param name="id">Definition identifier.</param>
    /// <param name="baseCost">Non-negative base cost.</param>
    /// <param name="preconditionFact">Optional precondition fact.</param>
    /// <param name="preconditionValue">Precondition value.</param>
    /// <param name="effectFact">Optional set-effect fact.</param>
    /// <param name="effectValue">Set-effect value.</param>
    /// <param name="clearFact">Optional clear-effect fact.</param>
    /// <returns>Immutable action definition.</returns>
    public static ActionDefinition CreateDefinition(
        ActionId id,
        int baseCost,
        WorldFactId? preconditionFact,
        int preconditionValue,
        WorldFactId? effectFact,
        int effectValue,
        WorldFactId? clearFact)
    {
        SymbolicWorldState preconditions = new();
        SymbolicWorldState effects = new();
        if (preconditionFact.HasValue)
        {
            _ = preconditions.Set(preconditionFact.Value, preconditionValue);
        }

        if (effectFact.HasValue)
        {
            _ = effects.Set(effectFact.Value, effectValue);
        }

        ulong clearMask = 0UL;
        if (clearFact.HasValue)
        {
            clearMask = 1UL << (int)clearFact.Value;
        }

        return new ActionDefinition(id, baseCost, preconditions, effects, clearMask);
    }

    /// <summary>
    /// Creates a definition with two precondition facts and one effect.
    /// </summary>
    public static ActionDefinition CreateDefinition(
        ActionId id,
        int baseCost,
        WorldFactId preconditionA,
        int valueA,
        WorldFactId preconditionB,
        int valueB,
        WorldFactId effectFact,
        int effectValue)
    {
        SymbolicWorldState preconditions = new();
        SymbolicWorldState effects = new();
        _ = preconditions.Set(preconditionA, valueA);
        _ = preconditions.Set(preconditionB, valueB);
        _ = effects.Set(effectFact, effectValue);
        return new ActionDefinition(id, baseCost, preconditions, effects, 0UL);
    }

    /// <summary>
    /// Creates a grounded candidate cloned from a definition template.
    /// </summary>
    public static ActionCandidate CreateCandidate(
        ActionDefinition definition,
        EntityId entity,
        TacticalPointId point,
        NavigationNodeId node,
        WeaponId weapon,
        OrderId order,
        SearchSectorId sector)
    {
        ArgumentNullException.ThrowIfNull(definition);
        SymbolicWorldState preconditions = new();
        SymbolicWorldState effects = new();
        _ = definition.PreconditionTemplate.CopyTo(preconditions);
        _ = definition.EffectSetTemplate.CopyTo(effects);

        return new ActionCandidate
        {
            DefinitionId = definition.Id,
            CandidateId = ActionCandidateId.Invalid,
            BoundEntityId = entity,
            BoundPointId = point,
            BoundNodeId = node,
            BoundWeaponId = weapon,
            BoundOrderId = order,
            BoundSectorId = sector,
            Preconditions = preconditions,
            EffectSets = effects,
            EffectClearMask = definition.EffectClearMask,
            Cost = definition.BaseCost,
            ValidationStatus = OperationStatus.Success,
        };
    }

    /// <summary>
    /// Adds a checked non-negative cost delta.
    /// </summary>
    public static OperationStatus AddCost(int baseCost, int delta, out int total)
    {
        total = 0;
        if (baseCost < 0 || delta < 0)
        {
            return OperationStatus.InvalidArgument;
        }

        try
        {
            total = checked(baseCost + delta);
        }
        catch (OverflowException)
        {
            return OperationStatus.ArithmeticOverflow;
        }

        return OperationStatus.Success;
    }
}
