namespace DynamicPlanningAI.Abstractions.Geometry;

/// <summary>
/// Eight-way facing and movement direction on the discrete grid.
/// </summary>
public enum Direction8 : byte
{
    /// <summary>
    /// North (0, -1).
    /// </summary>
    North = 0,

    /// <summary>
    /// Northeast (1, -1).
    /// </summary>
    NorthEast = 1,

    /// <summary>
    /// East (1, 0).
    /// </summary>
    East = 2,

    /// <summary>
    /// Southeast (1, 1).
    /// </summary>
    SouthEast = 3,

    /// <summary>
    /// South (0, 1).
    /// </summary>
    South = 4,

    /// <summary>
    /// Southwest (-1, 1).
    /// </summary>
    SouthWest = 5,

    /// <summary>
    /// West (-1, 0).
    /// </summary>
    West = 6,

    /// <summary>
    /// Northwest (-1, -1).
    /// </summary>
    NorthWest = 7,
}
