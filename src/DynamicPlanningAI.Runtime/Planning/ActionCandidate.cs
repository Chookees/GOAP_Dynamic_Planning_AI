using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.WorldState;

namespace DynamicPlanningAI.Runtime.Planning;

/// <summary>
/// Bound action candidate used by the planner. Stores identifiers and effect masks only.
/// </summary>
/// <remarks>
/// Preconditions and set-effects are owned as <see cref="SymbolicWorldState"/> instances
/// allocated during candidate generation (before freeze). Clear-effects use a bit mask.
/// </remarks>
public struct ActionCandidate
{
    /// <summary>
    /// Gets or sets the immutable action definition identifier.
    /// </summary>
    public ActionId DefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the workspace-local candidate identifier.
    /// </summary>
    public ActionCandidateId CandidateId { get; set; }

    /// <summary>
    /// Gets or sets the bound entity identifier, if any.
    /// </summary>
    public EntityId BoundEntityId { get; set; }

    /// <summary>
    /// Gets or sets the bound tactical point identifier, if any.
    /// </summary>
    public TacticalPointId BoundPointId { get; set; }

    /// <summary>
    /// Gets or sets the bound navigation node identifier, if any.
    /// </summary>
    public NavigationNodeId BoundNodeId { get; set; }

    /// <summary>
    /// Gets or sets the bound weapon identifier, if any.
    /// </summary>
    public WeaponId BoundWeaponId { get; set; }

    /// <summary>
    /// Gets or sets the bound order identifier, if any.
    /// </summary>
    public OrderId BoundOrderId { get; set; }

    /// <summary>
    /// Gets or sets the bound search sector identifier, if any.
    /// </summary>
    public SearchSectorId BoundSectorId { get; set; }

    /// <summary>
    /// Gets or sets the precondition state template.
    /// </summary>
    public SymbolicWorldState? Preconditions { get; set; }

    /// <summary>
    /// Gets or sets the set-effect state template.
    /// </summary>
    public SymbolicWorldState? EffectSets { get; set; }

    /// <summary>
    /// Gets or sets the clear-effect bit mask.
    /// </summary>
    public ulong EffectClearMask { get; set; }

    /// <summary>
    /// Gets or sets the non-negative action cost.
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// Gets or sets the candidate validation status.
    /// </summary>
    public OperationStatus ValidationStatus { get; set; }

    /// <summary>
    /// Gets a value indicating whether the candidate is usable by the planner.
    /// </summary>
    public readonly bool IsValid =>
        DefinitionId.IsValid &&
        ValidationStatus == OperationStatus.Success &&
        Cost >= 0 &&
        Preconditions is not null &&
        EffectSets is not null;
}
