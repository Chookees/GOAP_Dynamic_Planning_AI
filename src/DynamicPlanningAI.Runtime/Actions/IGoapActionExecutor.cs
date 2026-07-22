using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Planning;

namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Executes a grounded GOAP action candidate through Begin/Tick/Cancel.
/// </summary>
public interface IGoapActionExecutor
{
    /// <summary>
    /// Gets the action definition identifier handled by this executor.
    /// </summary>
    public Abstractions.Identifiers.ActionId DefinitionId { get; }

    /// <summary>
    /// Begins execution for the bound candidate.
    /// </summary>
    /// <param name="context">Mutable execution context.</param>
    /// <returns>Per-tick result after begin.</returns>
    public ActionTickResult Begin(ref ActionExecutionContext context);

    /// <summary>
    /// Advances execution by one tick.
    /// </summary>
    /// <param name="context">Mutable execution context.</param>
    /// <returns>Per-tick result.</returns>
    public ActionTickResult Tick(ref ActionExecutionContext context);

    /// <summary>
    /// Cancels an in-progress action.
    /// </summary>
    /// <param name="context">Mutable execution context.</param>
    /// <returns>Cancelled tick result.</returns>
    public ActionTickResult Cancel(ref ActionExecutionContext context);
}
