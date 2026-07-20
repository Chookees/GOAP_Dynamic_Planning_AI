namespace TacticalGoap.Abstractions.Facts;

/// <summary>
/// Blittable world-fact value supporting boolean, byte, and integer encodings.
/// </summary>
/// <remarks>
/// Symbolic facts must never store object references or floating-point values.
/// Continuous quantities are quantized into integer categories before assignment.
/// </remarks>
public readonly struct WorldFactValue
{
    private readonly int _value;

    /// <summary>
    /// Initializes a fact value from a signed integer.
    /// </summary>
    /// <param name="value">Integer fact payload.</param>
    public WorldFactValue(int value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a fact value from an unsigned byte.
    /// </summary>
    /// <param name="value">Byte fact payload.</param>
    public WorldFactValue(byte value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a fact value from a boolean.
    /// </summary>
    /// <param name="value">Boolean fact payload encoded as 1 or 0.</param>
    public WorldFactValue(bool value)
    {
        _value = value ? 1 : 0;
    }

    /// <summary>
    /// Boolean false fact value.
    /// </summary>
    public static WorldFactValue False { get; } = new(false);

    /// <summary>
    /// Boolean true fact value.
    /// </summary>
    public static WorldFactValue True { get; } = new(true);

    /// <summary>
    /// Gets the raw signed integer payload.
    /// </summary>
    public int AsInt32 => _value;

    /// <summary>
    /// Gets the payload truncated to an unsigned byte.
    /// </summary>
    public byte AsByte => unchecked((byte)_value);

    /// <summary>
    /// Gets a value indicating whether the payload is non-zero.
    /// </summary>
    public bool AsBool => _value != 0;

    /// <summary>
    /// Creates a fact value from a <see cref="DistanceCategory"/>.
    /// </summary>
    /// <param name="category">Distance category to encode.</param>
    /// <returns>Integer-encoded category value.</returns>
    public static WorldFactValue FromDistanceCategory(Geometry.DistanceCategory category)
    {
        return new WorldFactValue((int)category);
    }

    /// <summary>
    /// Attempts to interpret the payload as a <see cref="DistanceCategory"/>.
    /// </summary>
    /// <param name="category">Receives the category when valid.</param>
    /// <returns><see langword="true"/> when the payload is a defined category.</returns>
    public bool TryAsDistanceCategory(out Geometry.DistanceCategory category)
    {
        if (_value < 0 || _value > (int)Geometry.DistanceCategory.OutOfRange)
        {
            category = Geometry.DistanceCategory.OutOfRange;
            return false;
        }

        category = (Geometry.DistanceCategory)_value;
        return true;
    }
}
