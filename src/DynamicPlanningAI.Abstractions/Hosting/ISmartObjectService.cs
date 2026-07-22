using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Abstractions.Hosting;

/// <summary>
/// Host smart-object state queries.
/// </summary>
/// <remarks>
/// Reads must be deterministic within a tick for identical identifiers.
/// Implementations must not allocate on the frozen runtime path. Availability
/// reflects host-world state such as door open/closed or traversal readiness.
/// </remarks>
public interface ISmartObjectService
{
    /// <summary>
    /// Attempts to read a smart object cell position.
    /// </summary>
    /// <param name="smartObject">Smart object to locate.</param>
    /// <param name="position">Receives the position when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetPosition(SmartObjectId smartObject, out Int2 position);

    /// <summary>
    /// Returns whether the smart object is currently available for use.
    /// </summary>
    /// <param name="smartObject">Smart object to test.</param>
    /// <returns><see langword="true"/> when available.</returns>
    public bool IsAvailable(SmartObjectId smartObject);

    /// <summary>
    /// Attempts to read a host-defined state code for the smart object.
    /// </summary>
    /// <param name="smartObject">Smart object to query.</param>
    /// <param name="stateCode">Receives the state code when successful.</param>
    /// <returns>Success or not-found status.</returns>
    public OperationStatus TryGetStateCode(SmartObjectId smartObject, out int stateCode);
}
