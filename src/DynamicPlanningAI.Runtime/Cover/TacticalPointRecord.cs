using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Cover;

/// <summary>
/// Immutable tactical point record matching the cover evaluation model.
/// </summary>
public readonly struct TacticalPointRecord
{
    /// <summary>
    /// Initializes a tactical point record.
    /// </summary>
    /// <param name="id">Stable point identifier.</param>
    /// <param name="navigationNode">Linked navigation node.</param>
    /// <param name="position">Discrete cell position.</param>
    /// <param name="facing">Preferred facing direction.</param>
    /// <param name="category">Point category.</param>
    /// <param name="coverHeight">Cover height classification.</param>
    /// <param name="preferredStance">Preferred agent stance at the point.</param>
    /// <param name="exposure">Static exposure score (higher is more exposed).</param>
    /// <param name="quality">Static cover quality (higher is better).</param>
    /// <param name="area">Navigation area containing the point.</param>
    /// <param name="maxOccupants">Maximum simultaneous occupants.</param>
    public TacticalPointRecord(
        TacticalPointId id,
        NavigationNodeId navigationNode,
        Int2 position,
        Direction8 facing,
        TacticalPointCategory category,
        CoverHeight coverHeight,
        AgentStance preferredStance,
        int exposure,
        int quality,
        NavigationAreaId area,
        int maxOccupants)
    {
        Id = id;
        NavigationNode = navigationNode;
        Position = position;
        Facing = facing;
        Category = category;
        CoverHeight = coverHeight;
        PreferredStance = preferredStance;
        Exposure = exposure;
        Quality = quality;
        Area = area;
        MaxOccupants = maxOccupants;
    }

    /// <summary>
    /// Gets the stable point identifier.
    /// </summary>
    public TacticalPointId Id { get; }

    /// <summary>
    /// Gets the linked navigation node.
    /// </summary>
    public NavigationNodeId NavigationNode { get; }

    /// <summary>
    /// Gets the discrete cell position.
    /// </summary>
    public Int2 Position { get; }

    /// <summary>
    /// Gets the preferred facing direction.
    /// </summary>
    public Direction8 Facing { get; }

    /// <summary>
    /// Gets the point category.
    /// </summary>
    public TacticalPointCategory Category { get; }

    /// <summary>
    /// Gets the cover height classification.
    /// </summary>
    public CoverHeight CoverHeight { get; }

    /// <summary>
    /// Gets the preferred agent stance at the point.
    /// </summary>
    public AgentStance PreferredStance { get; }

    /// <summary>
    /// Gets the static exposure score (higher is more exposed).
    /// </summary>
    public int Exposure { get; }

    /// <summary>
    /// Gets the static cover quality (higher is better).
    /// </summary>
    public int Quality { get; }

    /// <summary>
    /// Gets the navigation area containing the point.
    /// </summary>
    public NavigationAreaId Area { get; }

    /// <summary>
    /// Gets the maximum simultaneous occupants.
    /// </summary>
    public int MaxOccupants { get; }
}
