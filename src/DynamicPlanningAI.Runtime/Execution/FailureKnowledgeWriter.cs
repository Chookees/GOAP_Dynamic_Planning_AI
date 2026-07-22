using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;

namespace DynamicPlanningAI.Runtime.Execution;

/// <summary>
/// Fixed-capacity sink that records action failure-knowledge hints.
/// </summary>
public sealed class FailureKnowledgeWriter : IFailureKnowledgeSink
{
    private readonly MemoryType[] _types;
    private readonly int[] _primaryIds;
    private readonly int[] _secondaryIds;
    private readonly long[] _ticks;
    private readonly AgentId[] _agents;
    private readonly int _capacity;
    private int _count;

    /// <summary>
    /// Initializes a writer with memory hard-limit capacity.
    /// </summary>
    public FailureKnowledgeWriter()
        : this(AiHardLimits.MaximumMemoryRecords)
    {
    }

    /// <summary>
    /// Initializes a writer with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Maximum retained hints.</param>
    public FailureKnowledgeWriter(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumMemoryRecords)
        {
            throw new System.ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
        _types = new MemoryType[capacity];
        _primaryIds = new int[capacity];
        _secondaryIds = new int[capacity];
        _ticks = new long[capacity];
        _agents = new AgentId[capacity];
        _count = 0;
    }

    /// <summary>Gets the number of recorded hints.</summary>
    public int Count => _count;

    /// <inheritdoc />
    public bool TryWrite(
        AgentId agentId,
        MemoryType memoryType,
        int primaryId,
        int secondaryId,
        long tickSequence)
    {
        if (!agentId.IsValid || _count >= _capacity)
        {
            return false;
        }

        _agents[_count] = agentId;
        _types[_count] = memoryType;
        _primaryIds[_count] = primaryId;
        _secondaryIds[_count] = secondaryId;
        _ticks[_count] = tickSequence;
        _count = checked(_count + 1);
        return true;
    }

    /// <summary>
    /// Attempts to read a recorded hint by index.
    /// </summary>
    /// <param name="index">Zero-based hint index.</param>
    /// <param name="agentId">Receives the agent.</param>
    /// <param name="memoryType">Receives the memory type.</param>
    /// <param name="primaryId">Receives the primary id.</param>
    /// <param name="secondaryId">Receives the secondary id.</param>
    /// <param name="tickSequence">Receives the tick.</param>
    /// <returns><see langword="true"/> when the index is valid.</returns>
    public bool TryGet(
        int index,
        out AgentId agentId,
        out MemoryType memoryType,
        out int primaryId,
        out int secondaryId,
        out long tickSequence)
    {
        if (index < 0 || index >= _count)
        {
            agentId = AgentId.Invalid;
            memoryType = MemoryType.TargetSeen;
            primaryId = 0;
            secondaryId = 0;
            tickSequence = 0L;
            return false;
        }

        agentId = _agents[index];
        memoryType = _types[index];
        primaryId = _primaryIds[index];
        secondaryId = _secondaryIds[index];
        tickSequence = _ticks[index];
        return true;
    }

    /// <summary>
    /// Clears recorded hints without releasing capacity.
    /// </summary>
    public void Reset()
    {
        _count = 0;
    }
}
