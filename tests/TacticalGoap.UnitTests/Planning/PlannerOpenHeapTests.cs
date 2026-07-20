using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;
using Xunit;

namespace TacticalGoap.UnitTests.Planning;

public sealed class PlannerOpenHeapTests
{
    [Fact]
    public void Insert_Peek_Pop_OrdersByTotalCost()
    {
        PlannerNode[] nodes = new PlannerNode[8];
        PlannerOpenHeap heap = new(8, nodes);

        nodes[0] = CreateNode(g: 30, h: 0, order: 0);
        nodes[1] = CreateNode(g: 10, h: 0, order: 1);
        nodes[2] = CreateNode(g: 20, h: 0, order: 2);

        Assert.Equal(OperationStatus.Success, heap.Insert(0));
        Assert.Equal(OperationStatus.Success, heap.Insert(1));
        Assert.Equal(OperationStatus.Success, heap.Insert(2));

        Assert.True(heap.TryPeek(out int peek));
        Assert.Equal(1, peek);

        Assert.True(heap.TryPop(out int first));
        Assert.Equal(1, first);
        Assert.True(heap.TryPop(out int second));
        Assert.Equal(2, second);
        Assert.True(heap.TryPop(out int third));
        Assert.Equal(0, third);
        Assert.False(heap.TryPop(out _));
    }

    [Fact]
    public void EqualCost_TieBreaksByInsertionOrderThenNodeIndex()
    {
        PlannerNode[] nodes = new PlannerNode[8];
        PlannerOpenHeap heap = new(8, nodes);

        nodes[3] = CreateNode(g: 5, h: 0, order: 2);
        nodes[1] = CreateNode(g: 5, h: 0, order: 0);
        nodes[2] = CreateNode(g: 5, h: 0, order: 1);

        Assert.Equal(OperationStatus.Success, heap.Insert(3));
        Assert.Equal(OperationStatus.Success, heap.Insert(1));
        Assert.Equal(OperationStatus.Success, heap.Insert(2));

        Assert.True(heap.TryPop(out int first));
        Assert.Equal(1, first);
        Assert.True(heap.TryPop(out int second));
        Assert.Equal(2, second);
        Assert.True(heap.TryPop(out int third));
        Assert.Equal(3, third);
    }

    [Fact]
    public void CapacityExceeded_IsExplicit()
    {
        PlannerNode[] nodes = new PlannerNode[2];
        PlannerOpenHeap heap = new(1, nodes);
        nodes[0] = CreateNode(1, 0, 0);
        nodes[1] = CreateNode(2, 0, 1);

        Assert.Equal(OperationStatus.Success, heap.Insert(0));
        Assert.Equal(OperationStatus.CapacityExceeded, heap.Insert(1));
    }

    [Fact]
    public void Clear_AllowsReuse()
    {
        PlannerNode[] nodes = new PlannerNode[4];
        PlannerOpenHeap heap = new(4, nodes);
        nodes[0] = CreateNode(1, 0, 0);
        Assert.Equal(OperationStatus.Success, heap.Insert(0));
        heap.Clear();
        Assert.True(heap.IsEmpty);
        Assert.Equal(OperationStatus.Success, heap.Insert(0));
        Assert.True(heap.TryPop(out int index));
        Assert.Equal(0, index);
    }

    private static PlannerNode CreateNode(int g, int h, int order)
    {
        return new PlannerNode
        {
            ParentIndex = -1,
            CandidateIndex = -1,
            GCost = g,
            HCost = h,
            StateHash = 0UL,
            StateIndex = 0,
            InsertionOrder = order,
        };
    }
}
