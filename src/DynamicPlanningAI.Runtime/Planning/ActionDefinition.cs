using System;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Planning;

/// <summary>
/// Immutable action definition metadata consumed during candidate generation.
/// </summary>
/// <remarks>
/// Runtime executors are supplied separately. This type only describes cost and
/// symbolic precondition/effect templates.
/// </remarks>
public sealed class ActionDefinition
{
    /// <summary>
    /// Initializes a new action definition.
    /// </summary>
    /// <param name="id">Stable definition identifier.</param>
    /// <param name="baseCost">Non-negative base cost.</param>
    /// <param name="preconditionTemplate">Precondition template (ownership transferred).</param>
    /// <param name="effectSetTemplate">Set-effect template (ownership transferred).</param>
    /// <param name="effectClearMask">Clear-effect bit mask.</param>
    public ActionDefinition(
        ActionId id,
        int baseCost,
        SymbolicWorldState preconditionTemplate,
        SymbolicWorldState effectSetTemplate,
        ulong effectClearMask)
    {
        ArgumentNullException.ThrowIfNull(preconditionTemplate);
        ArgumentNullException.ThrowIfNull(effectSetTemplate);

        if (!id.IsValid)
        {
            throw new ArgumentException("Action definition id must be valid.", nameof(id));
        }

        if (baseCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseCost), baseCost, "Base cost must be non-negative.");
        }

        Id = id;
        BaseCost = baseCost;
        PreconditionTemplate = preconditionTemplate;
        EffectSetTemplate = effectSetTemplate;
        EffectClearMask = effectClearMask;
    }

    /// <summary>
    /// Gets the definition identifier.
    /// </summary>
    public ActionId Id { get; }

    /// <summary>
    /// Gets the non-negative base cost.
    /// </summary>
    public int BaseCost { get; }

    /// <summary>
    /// Gets the precondition template.
    /// </summary>
    public SymbolicWorldState PreconditionTemplate { get; }

    /// <summary>
    /// Gets the set-effect template.
    /// </summary>
    public SymbolicWorldState EffectSetTemplate { get; }

    /// <summary>
    /// Gets the clear-effect bit mask.
    /// </summary>
    public ulong EffectClearMask { get; }
}
