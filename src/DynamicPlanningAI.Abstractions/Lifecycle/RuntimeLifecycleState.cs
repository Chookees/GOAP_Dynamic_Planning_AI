namespace DynamicPlanningAI.Abstractions.Lifecycle;

/// <summary>
/// Explicit runtime lifecycle states.
/// </summary>
/// <remarks>
/// Only the documented transitions are legal. Allocation is permitted during
/// configuration and initialization. After <see cref="Frozen"/>, the
/// runtime-critical path must not deliberately allocate.
/// </remarks>
public enum RuntimeLifecycleState : byte
{
    /// <summary>
    /// Runtime object constructed but not configured.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Accepting registration of agents, goals, actions, and tactical data.
    /// </summary>
    Configuring = 1,

    /// <summary>
    /// Storage allocated and validated; freeze not yet applied.
    /// </summary>
    Initialized = 2,

    /// <summary>
    /// Configuration sealed; ready for deterministic ticks.
    /// </summary>
    Frozen = 3,

    /// <summary>
    /// Actively processing host ticks.
    /// </summary>
    Running = 4,

    /// <summary>
    /// Stopped after running; may be reset for a new scenario.
    /// </summary>
    Stopped = 5,

    /// <summary>
    /// Disposed; no further operations are valid.
    /// </summary>
    Disposed = 6,
}
