using System;

namespace TacticalGoap.UnitTests.Contracts;

/// <summary>
/// Helper for measuring managed allocations on the current thread.
/// </summary>
/// <remarks>
/// Intended for sample and unit-test probes. Not used on the frozen runtime path.
/// </remarks>
public static class AllocationProbe
{
    /// <summary>
    /// Measures bytes allocated by <paramref name="action"/> on the current thread.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns>Non-negative allocated byte delta when available; otherwise zero.</returns>
    public static long Measure(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        long after = GC.GetAllocatedBytesForCurrentThread();
        long delta = after - before;
        return delta < 0L ? 0L : delta;
    }

    /// <summary>
    /// Measures bytes allocated by <paramref name="func"/> on the current thread.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="func">Function to execute.</param>
    /// <param name="result">Receives the function result.</param>
    /// <returns>Non-negative allocated byte delta when available; otherwise zero.</returns>
    public static long Measure<T>(Func<T> func, out T result)
    {
        ArgumentNullException.ThrowIfNull(func);

        long before = GC.GetAllocatedBytesForCurrentThread();
        result = func();
        long after = GC.GetAllocatedBytesForCurrentThread();
        long delta = after - before;
        return delta < 0L ? 0L : delta;
    }
}
