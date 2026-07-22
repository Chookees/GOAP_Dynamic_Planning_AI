using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Selection;

/// <summary>
/// Weapon candidate evaluated by <see cref="WeaponSelector"/>.
/// </summary>
public struct WeaponCandidate
{
    /// <summary>
    /// Gets or sets the weapon identifier.
    /// </summary>
    public WeaponId Weapon { get; set; }

    /// <summary>
    /// Gets or sets remaining ammunition rounds.
    /// </summary>
    public int Ammunition { get; set; }

    /// <summary>
    /// Gets or sets whether the weapon requires reload.
    /// </summary>
    public bool RequiresReload { get; set; }

    /// <summary>
    /// Gets or sets the preferred engagement distance category.
    /// </summary>
    public DistanceCategory PreferredRange { get; set; }

    /// <summary>
    /// Gets or sets whether the weapon is suitable for suppression.
    /// </summary>
    public bool SuppressionSuitable { get; set; }

    /// <summary>
    /// Gets or sets whether the weapon is a melee weapon.
    /// </summary>
    public bool IsMelee { get; set; }

    /// <summary>
    /// Gets or sets whether the weapon is a grenade.
    /// </summary>
    public bool IsGrenade { get; set; }

    /// <summary>
    /// Gets or sets the switch cost applied when not currently selected.
    /// </summary>
    public int SwitchCost { get; set; }
}

/// <summary>
/// Inputs for weapon selection.
/// </summary>
public struct WeaponSelectionRequest
{
    /// <summary>
    /// Gets or sets the selecting agent.
    /// </summary>
    public AgentId Agent { get; set; }

    /// <summary>
    /// Gets or sets the agent squad, when any.
    /// </summary>
    public SquadId Squad { get; set; }

    /// <summary>
    /// Gets or sets the current tick sequence.
    /// </summary>
    public long CurrentTick { get; set; }

    /// <summary>
    /// Gets or sets the distance category to the focus target.
    /// </summary>
    public DistanceCategory TargetRange { get; set; }

    /// <summary>
    /// Gets or sets the currently selected weapon for hysteresis.
    /// </summary>
    public WeaponId CurrentWeapon { get; set; }

    /// <summary>
    /// Gets or sets whether suppression is desired.
    /// </summary>
    public bool PreferSuppression { get; set; }

    /// <summary>
    /// Gets or sets whether melee is desired.
    /// </summary>
    public bool PreferMelee { get; set; }

    /// <summary>
    /// Gets or sets whether a grenade is desired.
    /// </summary>
    public bool PreferGrenade { get; set; }

    /// <summary>
    /// Gets or sets the sticky weapon bonus.
    /// </summary>
    public int HysteresisBonus { get; set; }
}

/// <summary>
/// Result of weapon selection.
/// </summary>
public readonly struct WeaponSelectionResult
{
    /// <summary>
    /// Initializes a weapon selection result.
    /// </summary>
    /// <param name="selected">Selected weapon.</param>
    /// <param name="score">Winning score.</param>
    /// <param name="changed">Whether the selection changed.</param>
    public WeaponSelectionResult(WeaponId selected, int score, bool changed)
    {
        Selected = selected;
        Score = score;
        Changed = changed;
    }

    /// <summary>
    /// Gets the selected weapon.
    /// </summary>
    public WeaponId Selected { get; }

    /// <summary>
    /// Gets the winning score.
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// Gets whether the selection changed.
    /// </summary>
    public bool Changed { get; }
}
