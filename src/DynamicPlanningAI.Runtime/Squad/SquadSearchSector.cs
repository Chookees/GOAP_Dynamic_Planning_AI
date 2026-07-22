using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Search sector visible to the squad coordinator for one tick.
/// </summary>
public readonly struct SquadSearchSector
{
    /// <summary>
    /// Initializes a search sector snapshot.
    /// </summary>
    /// <param name="sectorId">Sector identifier.</param>
    /// <param name="center">Sector center cell.</param>
    /// <param name="isClear">Whether the sector is already clear.</param>
    public SquadSearchSector(SearchSectorId sectorId, Int2 center, bool isClear)
    {
        SectorId = sectorId;
        Center = center;
        IsClear = isClear;
    }

    /// <summary>Gets the sector identifier.</summary>
    public SearchSectorId SectorId { get; }

    /// <summary>Gets the sector center cell.</summary>
    public Int2 Center { get; }

    /// <summary>Gets a value indicating whether the sector is clear.</summary>
    public bool IsClear { get; }
}
