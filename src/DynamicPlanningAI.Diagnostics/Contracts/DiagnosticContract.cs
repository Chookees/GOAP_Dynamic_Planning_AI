using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Contracts;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Diagnostics.Contracts;

/// <summary>
/// Thin re-export of <see cref="AiContract"/> helpers for diagnostic call sites.
/// </summary>
/// <remarks>
/// Prefer this wrapper from Diagnostics consumers so contract checks stay
/// allocation-free and consistent with the Abstractions contract surface.
/// </remarks>
public static class DiagnosticContract
{
    /// <summary>
    /// Validates a precondition and returns success or contract violation.
    /// </summary>
    /// <param name="condition">Condition that must hold before continuing.</param>
    /// <returns>
    /// <see cref="OperationStatus.Success"/> when <paramref name="condition"/> is
    /// <see langword="true"/>; otherwise <see cref="OperationStatus.ContractViolation"/>.
    /// </returns>
    [FrozenRuntimePath]
    public static OperationStatus Require(bool condition) => AiContract.Require(condition);

    /// <summary>
    /// Validates a postcondition and returns success or contract violation.
    /// </summary>
    /// <param name="condition">Condition that must hold after an operation.</param>
    /// <returns>
    /// <see cref="OperationStatus.Success"/> when <paramref name="condition"/> is
    /// <see langword="true"/>; otherwise <see cref="OperationStatus.ContractViolation"/>.
    /// </returns>
    [FrozenRuntimePath]
    public static OperationStatus Ensure(bool condition) => AiContract.Ensure(condition);

    /// <summary>
    /// Validates an invariant and returns success or contract violation.
    /// </summary>
    /// <param name="condition">Condition that must remain true across operations.</param>
    /// <returns>
    /// <see cref="OperationStatus.Success"/> when <paramref name="condition"/> is
    /// <see langword="true"/>; otherwise <see cref="OperationStatus.ContractViolation"/>.
    /// </returns>
    [FrozenRuntimePath]
    public static OperationStatus Invariant(bool condition) => AiContract.Invariant(condition);
}
