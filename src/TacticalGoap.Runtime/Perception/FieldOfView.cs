using TacticalGoap.Abstractions.Geometry;

namespace TacticalGoap.Runtime.Perception;

/// <summary>
/// Discrete field-of-view helpers for vision filtering.
/// </summary>
internal static class FieldOfView
{
    /// <summary>
    /// Returns whether <paramref name="target"/> lies within a three-octant forward cone.
    /// </summary>
    /// <param name="origin">Observer cell.</param>
    /// <param name="facing">Observer facing.</param>
    /// <param name="target">Target cell.</param>
    /// <returns><see langword="true"/> when inside the forward cone.</returns>
    public static bool IsInForwardCone(Int2 origin, Direction8 facing, Int2 target)
    {
        int dx = target.X - origin.X;
        int dy = target.Y - origin.Y;
        if (dx == 0 && dy == 0)
        {
            return true;
        }

        Direction8 toward = ClassifyDirection(dx, dy);
        int facingValue = (int)facing;
        int towardValue = (int)toward;
        int delta = towardValue - facingValue;
        if (delta < 0)
        {
            delta = -delta;
        }

        if (delta > 4)
        {
            delta = 8 - delta;
        }

        return delta <= 1;
    }

    /// <summary>
    /// Classifies a delta into the nearest <see cref="Direction8"/>.
    /// </summary>
    /// <param name="dx">Horizontal delta.</param>
    /// <param name="dy">Vertical delta.</param>
    /// <returns>Nearest eight-way direction.</returns>
    public static Direction8 ClassifyDirection(int dx, int dy)
    {
        int sx = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int sy = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        if (sx == 0 && sy < 0)
        {
            return Direction8.North;
        }

        if (sx > 0 && sy < 0)
        {
            return Direction8.NorthEast;
        }

        if (sx > 0 && sy == 0)
        {
            return Direction8.East;
        }

        if (sx > 0 && sy > 0)
        {
            return Direction8.SouthEast;
        }

        if (sx == 0 && sy > 0)
        {
            return Direction8.South;
        }

        if (sx < 0 && sy > 0)
        {
            return Direction8.SouthWest;
        }

        if (sx < 0 && sy == 0)
        {
            return Direction8.West;
        }

        return Direction8.NorthWest;
    }
}
