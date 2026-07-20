using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Abstractions.Contracts;

/// <summary>
/// Allocation-free runtime contract helpers for the frozen execution path.
/// </summary>
/// <remarks>
/// These helpers never throw and never allocate. They return
/// <see cref="OperationStatus.ContractViolation"/> when a condition fails so
/// callers can fail closed without exceptions on the frozen runtime path.
/// Prefer <see cref="Require"/> at API boundaries, <see cref="Ensure"/> for
/// postconditions, and <see cref="Invariant"/> for maintained internal facts.
/// </remarks>
public static class AiContract
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
    public static OperationStatus Require(bool condition)
    {
        if (!condition)
        {
            return OperationStatus.ContractViolation;
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Validates a postcondition and returns success or contract violation.
    /// </summary>
    /// <param name="condition">Condition that must hold after an operation.</param>
    /// <returns>
    /// <see cref="OperationStatus.Success"/> when <paramref name="condition"/> is
    /// <see langword="true"/>; otherwise <see cref="OperationStatus.ContractViolation"/>.
    /// </returns>
    [FrozenRuntimePath]
    public static OperationStatus Ensure(bool condition)
    {
        if (!condition)
        {
            return OperationStatus.ContractViolation;
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Validates an invariant and returns success or contract violation.
    /// </summary>
    /// <param name="condition">Condition that must remain true across operations.</param>
    /// <returns>
    /// <see cref="OperationStatus.Success"/> when <paramref name="condition"/> is
    /// <see langword="true"/>; otherwise <see cref="OperationStatus.ContractViolation"/>.
    /// </returns>
    [FrozenRuntimePath]
    public static OperationStatus Invariant(bool condition)
    {
        if (!condition)
        {
            return OperationStatus.ContractViolation;
        }

        return OperationStatus.Success;
    }
}
