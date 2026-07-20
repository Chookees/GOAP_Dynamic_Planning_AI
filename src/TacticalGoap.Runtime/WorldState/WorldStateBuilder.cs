using System;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.WorldState;

/// <summary>
/// Builds a <see cref="SymbolicWorldState"/> from bounded agent-local snapshots.
/// </summary>
/// <remarks>
/// The builder never reads omniscient world data. Callers supply only facts the
/// agent can observe through body, memory, orders, and danger channels.
/// </remarks>
public sealed class WorldStateBuilder
{
    private readonly SymbolicWorldState _scratch;

    /// <summary>
    /// Initializes a builder with a private scratch state.
    /// </summary>
    public WorldStateBuilder()
    {
        _scratch = new SymbolicWorldState();
    }

    /// <summary>
    /// Builds a symbolic state into <paramref name="destination"/> from agent-local flags.
    /// </summary>
    /// <param name="bodyFlags">Body/posture fact mask; bit index matches <see cref="WorldFactId"/>.</param>
    /// <param name="bodyValues">Values for bits set in <paramref name="bodyFlags"/>.</param>
    /// <param name="memoryFlags">Memory-derived fact mask.</param>
    /// <param name="memoryValues">Values for bits set in <paramref name="memoryFlags"/>.</param>
    /// <param name="orderFlags">Order-derived fact mask.</param>
    /// <param name="orderValues">Values for bits set in <paramref name="orderFlags"/>.</param>
    /// <param name="dangerFlags">Danger-derived fact mask.</param>
    /// <param name="dangerValues">Values for bits set in <paramref name="dangerFlags"/>.</param>
    /// <param name="destination">Preallocated destination state.</param>
    /// <returns>Success or validation failure.</returns>
    public static OperationStatus Build(
        ulong bodyFlags,
        ReadOnlySpan<int> bodyValues,
        ulong memoryFlags,
        ReadOnlySpan<int> memoryValues,
        ulong orderFlags,
        ReadOnlySpan<int> orderValues,
        ulong dangerFlags,
        ReadOnlySpan<int> dangerValues,
        SymbolicWorldState destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        destination.Reset();
        OperationStatus status = ApplyChannel(bodyFlags, bodyValues, destination);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = ApplyChannel(memoryFlags, memoryValues, destination);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = ApplyChannel(orderFlags, orderValues, destination);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return ApplyChannel(dangerFlags, dangerValues, destination);
    }

    /// <summary>
    /// Builds into the scratch buffer and copies into <paramref name="destination"/>.
    /// </summary>
    /// <param name="bodyFlags">Body fact mask.</param>
    /// <param name="bodyValues">Body values.</param>
    /// <param name="memoryFlags">Memory fact mask.</param>
    /// <param name="memoryValues">Memory values.</param>
    /// <param name="orderFlags">Order fact mask.</param>
    /// <param name="orderValues">Order values.</param>
    /// <param name="dangerFlags">Danger fact mask.</param>
    /// <param name="dangerValues">Danger values.</param>
    /// <param name="destination">Destination state.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus BuildViaScratch(
        ulong bodyFlags,
        ReadOnlySpan<int> bodyValues,
        ulong memoryFlags,
        ReadOnlySpan<int> memoryValues,
        ulong orderFlags,
        ReadOnlySpan<int> orderValues,
        ulong dangerFlags,
        ReadOnlySpan<int> dangerValues,
        SymbolicWorldState destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        OperationStatus status = Build(
            bodyFlags,
            bodyValues,
            memoryFlags,
            memoryValues,
            orderFlags,
            orderValues,
            dangerFlags,
            dangerValues,
            _scratch);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return _scratch.CopyTo(destination);
    }

    private static OperationStatus ApplyChannel(
        ulong flags,
        ReadOnlySpan<int> values,
        SymbolicWorldState destination)
    {
        int valueIndex = 0;
        ulong remaining = flags;
        while (remaining != 0UL)
        {
            int factIndex = System.Numerics.BitOperations.TrailingZeroCount(remaining);
            ulong bit = 1UL << factIndex;
            remaining &= ~bit;

            if (valueIndex >= values.Length)
            {
                return OperationStatus.InvalidArgument;
            }

            OperationStatus status = destination.Set((WorldFactId)factIndex, values[valueIndex]);
            if (status != OperationStatus.Success)
            {
                return status;
            }

            valueIndex++;
        }

        return OperationStatus.Success;
    }
}
