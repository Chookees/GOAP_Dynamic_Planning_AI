using System;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Limits;

namespace DynamicPlanningAI.Sample.World;

/// <summary>
/// Discrete cell kinds for the sample grid world.
/// </summary>
public enum CellKind : byte
{
    /// <summary>Open floor.</summary>
    Floor = 0,

    /// <summary>Solid wall.</summary>
    Wall = 1,

    /// <summary>Closed or open door cell.</summary>
    Door = 2,

    /// <summary>Traversable window cell.</summary>
    Window = 3,

    /// <summary>Cover marker overlay on floor.</summary>
    Cover = 4,
}

/// <summary>
/// Bounded 2D grid map with walls, doors, windows, and cover markers.
/// </summary>
public sealed class GridMap
{
    private readonly CellKind[] _cells;
    private readonly byte[] _doorFlags;
    private readonly int _width;
    private readonly int _height;
    private readonly int _cellCount;

    /// <summary>
    /// Initializes a grid filled with floor cells.
    /// </summary>
    /// <param name="width">Inclusive width in 1..128.</param>
    /// <param name="height">Inclusive height in 1..128.</param>
    public GridMap(int width, int height)
    {
        if (width < 1 || width > 128 || height < 1 || height > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be in 1..128.");
        }

        _width = width;
        _height = height;
        _cellCount = checked(width * height);
        _cells = new CellKind[_cellCount];
        _doorFlags = new byte[_cellCount];
        for (int i = 0; i < _cellCount; i++)
        {
            _cells[i] = CellKind.Floor;
            _doorFlags[i] = 0;
        }
    }

    /// <summary>Gets the grid width.</summary>
    public int Width => _width;

    /// <summary>Gets the grid height.</summary>
    public int Height => _height;

    /// <summary>Gets the total cell count.</summary>
    public int CellCount => _cellCount;

    /// <summary>
    /// Converts a cell to a navigation node index.
    /// </summary>
    public int ToIndex(Int2 cell) => checked((cell.Y * _width) + cell.X);

    /// <summary>
    /// Converts a navigation node index to a cell.
    /// </summary>
    public Int2 ToCell(int index)
    {
        if (index < 0 || index >= _cellCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new Int2(index % _width, index / _width);
    }

    /// <summary>
    /// Returns whether a cell is inside the grid.
    /// </summary>
    public bool InBounds(Int2 cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < _width && cell.Y < _height;

    /// <summary>
    /// Gets the cell kind at a position.
    /// </summary>
    public CellKind GetKind(Int2 cell)
    {
        if (!InBounds(cell))
        {
            return CellKind.Wall;
        }

        return _cells[ToIndex(cell)];
    }

    /// <summary>
    /// Sets the cell kind at a position.
    /// </summary>
    public void SetKind(Int2 cell, CellKind kind)
    {
        if (!InBounds(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        _cells[ToIndex(cell)] = kind;
    }

    /// <summary>
    /// Fills a rectangle with a cell kind (inclusive bounds).
    /// </summary>
    public void FillRect(int x0, int y0, int x1, int y1, CellKind kind)
    {
        int maxX = x1 < _width ? x1 : _width - 1;
        int maxY = y1 < _height ? y1 : _height - 1;
        int minX = x0 < 0 ? 0 : x0;
        int minY = y0 < 0 ? 0 : y0;
        for (int y = minY; y <= maxY && y < AiHardLimits.MaximumTacticalPoints; y++)
        {
            for (int x = minX; x <= maxX && x < AiHardLimits.MaximumTacticalPoints; x++)
            {
                _cells[ToIndex(new Int2(x, y))] = kind;
            }
        }
    }

    /// <summary>
    /// Places a door with optional blocked/breachable flags.
    /// </summary>
    /// <param name="cell">Door cell.</param>
    /// <param name="isOpen">Whether the door is currently open.</param>
    /// <param name="isBlocked">Whether open attempts fail.</param>
    /// <param name="isBreachable">Whether breach is allowed.</param>
    public void PlaceDoor(Int2 cell, bool isOpen, bool isBlocked, bool isBreachable)
    {
        SetKind(cell, CellKind.Door);
        byte flags = 0;
        if (isOpen)
        {
            flags |= 1;
        }

        if (isBlocked)
        {
            flags |= 2;
        }

        if (isBreachable)
        {
            flags |= 4;
        }

        _doorFlags[ToIndex(cell)] = flags;
    }

    /// <summary>Returns whether the door is open.</summary>
    public bool IsDoorOpen(Int2 cell) => InBounds(cell) && (_doorFlags[ToIndex(cell)] & 1) != 0;

    /// <summary>Returns whether the door is blocked.</summary>
    public bool IsDoorBlocked(Int2 cell) => InBounds(cell) && (_doorFlags[ToIndex(cell)] & 2) != 0;

    /// <summary>Returns whether the door is breachable.</summary>
    public bool IsDoorBreachable(Int2 cell) => InBounds(cell) && (_doorFlags[ToIndex(cell)] & 4) != 0;

    /// <summary>Opens a door and clears the blocked flag.</summary>
    public void OpenDoor(Int2 cell)
    {
        if (!InBounds(cell) || GetKind(cell) != CellKind.Door)
        {
            return;
        }

        int index = ToIndex(cell);
        _doorFlags[index] = (byte)((_doorFlags[index] | 1) & ~2);
    }

    /// <summary>Breaches a blocked door, opening it.</summary>
    public bool TryBreachDoor(Int2 cell)
    {
        if (!InBounds(cell) || GetKind(cell) != CellKind.Door || !IsDoorBreachable(cell))
        {
            return false;
        }

        OpenDoor(cell);
        return true;
    }

    /// <summary>
    /// Returns whether a cell is walkable for pathfinding.
    /// </summary>
    /// <param name="cell">Cell to test.</param>
    /// <param name="allowWindow">Whether window cells are traversable.</param>
    public bool IsWalkable(Int2 cell, bool allowWindow)
    {
        if (!InBounds(cell))
        {
            return false;
        }

        CellKind kind = GetKind(cell);
        if (kind == CellKind.Wall)
        {
            return false;
        }

        if (kind == CellKind.Door)
        {
            return IsDoorOpen(cell);
        }

        if (kind == CellKind.Window)
        {
            return allowWindow;
        }

        return true;
    }
}
