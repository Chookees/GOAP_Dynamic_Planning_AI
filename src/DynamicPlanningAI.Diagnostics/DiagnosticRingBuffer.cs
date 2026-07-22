using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Diagnostics;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Limits;

namespace DynamicPlanningAI.Diagnostics;

/// <summary>
/// Fixed-capacity ring buffer of blittable <see cref="TraceRecord"/> values.
/// </summary>
/// <remarks>
/// Writes overwrite the oldest record when full and bump <see cref="DroppedCount"/>.
/// This type is safe for the frozen runtime path: it never allocates after
/// construction. String formatting belongs in on-demand formatters, not here.
/// </remarks>
public sealed class DiagnosticRingBuffer : IRuntimeTraceSink
{
    private readonly TraceRecord[] _records;
    private int _writeIndex;
    private int _count;
    private long _droppedCount;

    /// <summary>
    /// Initializes a ring buffer with hard-limit capacity.
    /// </summary>
    public DiagnosticRingBuffer()
        : this(AiHardLimits.MaximumDiagnosticRecords)
    {
    }

    /// <summary>
    /// Initializes a ring buffer with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Inclusive capacity in 1..<see cref="AiHardLimits.MaximumDiagnosticRecords"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is invalid.</exception>
    public DiagnosticRingBuffer(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumDiagnosticRecords)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be in 1..MaximumDiagnosticRecords.");
        }

        _records = new TraceRecord[capacity];
        _writeIndex = 0;
        _count = 0;
        _droppedCount = 0L;
    }

    /// <summary>
    /// Gets the fixed capacity.
    /// </summary>
    public int Capacity => _records.Length;

    /// <summary>
    /// Gets the number of occupied records currently readable.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the number of writes discarded because the buffer was full.
    /// </summary>
    public long DroppedCount => _droppedCount;

    /// <summary>
    /// Writes a record, overwriting the oldest entry when full.
    /// </summary>
    /// <param name="record">Blittable trace record.</param>
    [FrozenRuntimePath]
    public void Write(in TraceRecord record)
    {
        _records[_writeIndex] = record;
        _writeIndex++;
        if (_writeIndex >= _records.Length)
        {
            _writeIndex = 0;
        }

        if (_count < _records.Length)
        {
            _count++;
            return;
        }

        _droppedCount++;
    }

    /// <summary>
    /// Attempts to read a record by age-relative index (0 = oldest).
    /// </summary>
    /// <param name="index">Zero-based index from the oldest retained record.</param>
    /// <param name="record">Receives the record when successful.</param>
    /// <returns><see langword="true"/> when <paramref name="index"/> is in range.</returns>
    [FrozenRuntimePath]
    public bool TryRead(int index, out TraceRecord record)
    {
        if (index < 0 || index >= _count)
        {
            record = default;
            return false;
        }

        int start = _writeIndex - _count;
        if (start < 0)
        {
            start += _records.Length;
        }

        int absolute = start + index;
        if (absolute >= _records.Length)
        {
            absolute -= _records.Length;
        }

        record = _records[absolute];
        return true;
    }

    /// <summary>
    /// Clears all retained records and resets the write cursor.
    /// </summary>
    /// <remarks>
    /// Does not reset <see cref="DroppedCount"/> so overflow history remains visible.
    /// </remarks>
    [FrozenRuntimePath]
    public void Clear()
    {
        for (int i = 0; i < _records.Length; i++)
        {
            _records[i] = default;
        }

        _writeIndex = 0;
        _count = 0;
    }

    /// <summary>
    /// Resets retained records and the dropped counter.
    /// </summary>
    [FrozenRuntimePath]
    public void ResetStatistics()
    {
        Clear();
        _droppedCount = 0L;
    }
}
