using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Abstractions.Ticks;

namespace DynamicPlanningAI.Runtime;

/// <summary>
/// Optional perception hook invoked once per agent tick before arbitration.
/// </summary>
public interface IPerceptionTickHook
{
    /// <summary>
    /// Runs perception for an agent.
    /// </summary>
    /// <param name="tick">Current tick.</param>
    /// <param name="agentId">Agent identifier.</param>
    /// <returns>Operation status.</returns>
    public OperationStatus Tick(in AiTick tick, AgentId agentId);
}

/// <summary>
/// Optional squad coordination hook invoked after plan execution.
/// </summary>
public interface ISquadTickHook
{
    /// <summary>
    /// Runs squad coordination for the current tick.
    /// </summary>
    /// <param name="tick">Current tick.</param>
    /// <returns>Operation status.</returns>
    public OperationStatus Tick(in AiTick tick);
}

/// <summary>
/// Optional diagnostics writer for runtime tick events.
/// </summary>
public interface IRuntimeDiagnosticsSink
{
    /// <summary>
    /// Writes a bounded diagnostic record.
    /// </summary>
    /// <param name="result">Operation result payload.</param>
    public void Write(in OperationResult result);
}
