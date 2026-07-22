using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DynamicPlanningAI.Abstractions.Geometry;

/// <summary>
/// Immutable two-dimensional integer coordinate used by host spatial contracts.
/// </summary>
/// <remarks>
/// All arithmetic that may overflow must use checked operators. Coordinates are
/// discrete grid cells; floating-point positions are quantized by the host before
/// crossing the abstraction boundary.
/// </remarks>
public readonly struct Int2 : IEquatable<Int2>
{
    /// <summary>
    /// Origin coordinate at (0, 0).
    /// </summary>
    public static readonly Int2 Zero = new(0, 0);

    /// <summary>
    /// Initializes a new integer coordinate.
    /// </summary>
    /// <param name="x">Horizontal cell coordinate.</param>
    /// <param name="y">Vertical cell coordinate.</param>
    public Int2(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the horizontal cell coordinate.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the vertical cell coordinate.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Computes the Manhattan distance between two coordinates.
    /// </summary>
    /// <param name="a">First coordinate.</param>
    /// <param name="b">Second coordinate.</param>
    /// <returns>Absolute horizontal plus absolute vertical delta.</returns>
    public static int ManhattanDistance(Int2 a, Int2 b)
    {
        int dx = a.X >= b.X ? a.X - b.X : b.X - a.X;
        int dy = a.Y >= b.Y ? a.Y - b.Y : b.Y - a.Y;
        return checked(dx + dy);
    }

    /// <summary>
    /// Adds two coordinates using checked arithmetic.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Component-wise sum.</returns>
    /// <exception cref="OverflowException">Thrown when either component overflows.</exception>
    public static Int2 Add(Int2 left, Int2 right)
    {
        return new Int2(checked(left.X + right.X), checked(left.Y + right.Y));
    }

    /// <summary>
    /// Subtracts two coordinates using checked arithmetic.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Component-wise difference.</returns>
    /// <exception cref="OverflowException">Thrown when either component overflows.</exception>
    public static Int2 Subtract(Int2 left, Int2 right)
    {
        return new Int2(checked(left.X - right.X), checked(left.Y - right.Y));
    }

    /// <summary>
    /// Adds two coordinates using checked arithmetic.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Component-wise sum.</returns>
    /// <exception cref="OverflowException">Thrown when either component overflows.</exception>
    public static Int2 operator +(Int2 left, Int2 right) => Add(left, right);

    /// <summary>
    /// Subtracts two coordinates using checked arithmetic.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Component-wise difference.</returns>
    /// <exception cref="OverflowException">Thrown when either component overflows.</exception>
    public static Int2 operator -(Int2 left, Int2 right) => Subtract(left, right);

    /// <inheritdoc />
    public bool Equals(Int2 other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Int2 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <inheritdoc />
    public override string ToString() =>
        string.Concat(
            "(",
            X.ToString(CultureInfo.InvariantCulture),
            ", ",
            Y.ToString(CultureInfo.InvariantCulture),
            ")");

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><see langword="true"/> when both components are equal.</returns>
    public static bool operator ==(Int2 left, Int2 right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><see langword="true"/> when either component differs.</returns>
    public static bool operator !=(Int2 left, Int2 right) => !left.Equals(right);
}
