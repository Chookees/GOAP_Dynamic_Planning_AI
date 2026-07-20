using System;
using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Memory;

namespace TacticalGoap.Runtime.Selection;

/// <summary>
/// Result of squad-order selection.
/// </summary>
public readonly struct OrderSelectionResult
{
    /// <summary>
    /// Initializes an order selection result.
    /// </summary>
    /// <param name="order">Selected order identifier.</param>
    /// <param name="relatedEntity">Related entity from the order memory.</param>
    /// <param name="found">Whether an order was selected.</param>
    public OrderSelectionResult(OrderId order, EntityId relatedEntity, bool found)
    {
        Order = order;
        RelatedEntity = relatedEntity;
        Found = found;
    }

    /// <summary>
    /// Gets the selected order identifier.
    /// </summary>
    public OrderId Order { get; }

    /// <summary>
    /// Gets the related entity from the order memory.
    /// </summary>
    public EntityId RelatedEntity { get; }

    /// <summary>
    /// Gets whether an order was found.
    /// </summary>
    public bool Found { get; }
}

/// <summary>
/// Bounded order selector over squad-order memory records.
/// </summary>
public sealed class OrderSelector
{
    private readonly MemoryRecord[] _scratch;

    /// <summary>
    /// Initializes the selector with a memory scratch buffer.
    /// </summary>
    public OrderSelector()
    {
        _scratch = new MemoryRecord[AiHardLimits.MaximumMemoryRecords];
    }

    /// <summary>
    /// Selects the most recent squad order within a candidate bound.
    /// </summary>
    /// <param name="memory">Agent working memory.</param>
    /// <param name="maxCandidates">Maximum order records considered.</param>
    /// <returns>Selection result.</returns>
    [FrozenRuntimePath]
    public OrderSelectionResult Select(WorkingMemoryStore memory, int maxCandidates)
    {
        ArgumentNullException.ThrowIfNull(memory);

        Span<MemoryRecord> scratch = _scratch.AsSpan();
        if (memory.CopyTo(scratch, out int count) != OperationStatus.Success)
        {
            return new OrderSelectionResult(OrderId.Invalid, EntityId.Invalid, false);
        }

        int limit = maxCandidates;
        if (limit <= 0 || limit > AiHardLimits.MaximumSquadOrders)
        {
            limit = AiHardLimits.MaximumSquadOrders;
        }

        OrderId bestOrder = OrderId.Invalid;
        EntityId bestRelated = EntityId.Invalid;
        long bestUpdate = -1L;
        int bestOrderValue = int.MaxValue;
        int considered = 0;
        bool found = false;

        for (int i = 0; i < count && considered < limit; i++)
        {
            ref MemoryRecord record = ref scratch[i];
            if (record.Type != MemoryType.SquadOrderReceived || record.Payload1 < 0)
            {
                continue;
            }

            considered = checked(considered + 1);
            int orderValue = record.Payload1;
            if (!found
                || record.UpdateTick > bestUpdate
                || (record.UpdateTick == bestUpdate && orderValue < bestOrderValue))
            {
                found = true;
                bestUpdate = record.UpdateTick;
                bestOrderValue = orderValue;
                bestOrder = OrderId.FromInt32(orderValue);
                bestRelated = record.RelatedEntity;
            }
        }

        return new OrderSelectionResult(bestOrder, bestRelated, found);
    }
}
