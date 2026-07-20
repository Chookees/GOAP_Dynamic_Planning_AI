using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Runtime.Execution;

/// <summary>
/// Receives bounded failure-knowledge hints written by action executors.
/// </summary>
public interface IFailureKnowledgeSink
{
    /// <summary>
    /// Records a failure-knowledge hint for an agent.
    /// </summary>
    /// <param name="agentId">Agent that experienced the failure.</param>
    /// <param name="memoryType">Memory type describing the failure.</param>
    /// <param name="primaryId">Primary related identifier value.</param>
    /// <param name="secondaryId">Secondary related identifier value.</param>
    /// <param name="tickSequence">Tick when the failure occurred.</param>
    /// <returns><see langword="true"/> when the hint was accepted.</returns>
    public bool TryWrite(
        AgentId agentId,
        MemoryType memoryType,
        int primaryId,
        int secondaryId,
        long tickSequence);
}
