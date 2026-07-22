using System;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Runtime.Navigation;
using Xunit;

namespace DynamicPlanningAI.UnitTests.Navigation;

public sealed class NavigationOrchestratorTests
{
    [Fact]
    public void RequestPath_CapacityExceeded_WhenHostReportsOverflow()
    {
        FakeNavigationService service = new(NavigationQueryStatus.CapacityExceeded, pathLength: 0);
        RecordingFailureHook hook = new();
        NavigationOrchestrator orchestrator = new(hook);
        NavigationPathBuffer buffer = new(capacity: 4);
        PathFollowState state = default;
        NavigationPathQuery query = CreateQuery();

        NavigationQueryResult result = orchestrator.RequestPath(
            service,
            query,
            NavigationQueryFlags.None,
            buffer,
            ref state,
            tickSequence: 1);

        Assert.Equal(NavigationQueryStatus.CapacityExceeded, result.Status);
        Assert.Equal(PathFollowStatus.CapacityExceeded, state.Status);
        Assert.True(buffer.IsEmpty);
        Assert.Equal(1, hook.FailureCount);
        Assert.Equal(NavigationQueryStatus.CapacityExceeded, hook.LastStatus);
    }

    [Fact]
    public void RequestPath_NoPath_RecordsFailureAndSetsStatus()
    {
        FakeNavigationService service = new(NavigationQueryStatus.NoPath, pathLength: 0);
        RecordingFailureHook hook = new();
        NavigationOrchestrator orchestrator = new(hook);
        NavigationPathBuffer buffer = new(capacity: 8);
        PathFollowState state = default;

        NavigationQueryResult result = orchestrator.RequestPath(
            service,
            CreateQuery(),
            NavigationQueryFlags.AllowDoorLinks,
            buffer,
            ref state,
            tickSequence: 3);

        Assert.Equal(NavigationQueryStatus.NoPath, result.Status);
        Assert.Equal(PathFollowStatus.NoPath, state.Status);
        Assert.Equal(1, hook.FailureCount);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void RequestPath_Success_FollowsAndArrivesDeterministically()
    {
        FakeNavigationService service = new(NavigationQueryStatus.Success, pathLength: 3);
        NavigationOrchestrator orchestrator = new();
        NavigationPathBuffer buffer = new(capacity: 8);
        PathFollowState state = default;

        NavigationQueryResult first = orchestrator.RequestPath(
            service,
            CreateQuery(),
            NavigationQueryFlags.AllowAllTraversalLinks,
            buffer,
            ref state,
            tickSequence: 1);

        NavigationQueryResult second = orchestrator.RequestPath(
            service,
            CreateQuery(),
            NavigationQueryFlags.AllowAllTraversalLinks,
            buffer,
            ref state,
            tickSequence: 2);

        Assert.Equal(NavigationQueryStatus.Success, first.Status);
        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.PathLength, second.PathLength);
        Assert.Equal(3, buffer.Count);
        Assert.Equal(PathFollowStatus.Following, state.Status);

        Assert.Equal(PathFollowStatus.Following, NavigationOrchestrator.AdvanceFollow(ref state, reachedCurrent: true));
        Assert.Equal(PathFollowStatus.Following, NavigationOrchestrator.AdvanceFollow(ref state, reachedCurrent: true));
        Assert.Equal(PathFollowStatus.Arrived, NavigationOrchestrator.AdvanceFollow(ref state, reachedCurrent: true));
    }

    [Fact]
    public void RequestPath_RejectsDisallowedDoorLink()
    {
        FakeNavigationService service = new(NavigationQueryStatus.Success, pathLength: 2);
        DoorLinkClassifier classifier = new();
        NavigationOrchestrator orchestrator = new(failureHook: null, classifier);
        NavigationPathBuffer buffer = new(capacity: 4);
        PathFollowState state = default;

        NavigationQueryResult result = orchestrator.RequestPath(
            service,
            CreateQuery(),
            NavigationQueryFlags.None,
            buffer,
            ref state,
            tickSequence: 1);

        Assert.Equal(NavigationQueryStatus.HostRejected, result.Status);
        Assert.Equal(PathFollowStatus.Failed, state.Status);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void RequestPath_AllowsDoorLinkWhenFlagSet()
    {
        FakeNavigationService service = new(NavigationQueryStatus.Success, pathLength: 2);
        DoorLinkClassifier classifier = new();
        NavigationOrchestrator orchestrator = new(failureHook: null, classifier);
        NavigationPathBuffer buffer = new(capacity: 4);
        PathFollowState state = default;

        NavigationQueryResult result = orchestrator.RequestPath(
            service,
            CreateQuery(),
            NavigationQueryFlags.AllowDoorLinks,
            buffer,
            ref state,
            tickSequence: 1);

        Assert.Equal(NavigationQueryStatus.Success, result.Status);
        Assert.True(state.RequiresDoorLink);
        Assert.Equal(PathFollowStatus.Following, state.Status);
    }

    private static NavigationPathQuery CreateQuery()
    {
        return new NavigationPathQuery(
            AgentId.FromInt32(0),
            NavigationNodeId.FromInt32(0),
            NavigationNodeId.FromInt32(2),
            Int2.Zero,
            new Int2(2, 0),
            maximumCost: 0,
            allowPartial: false);
    }

    private sealed class FakeNavigationService : INavigationService
    {
        private readonly NavigationQueryStatus _status;
        private readonly int _pathLength;

        public FakeNavigationService(NavigationQueryStatus status, int pathLength)
        {
            _status = status;
            _pathLength = pathLength;
        }

        public NavigationQueryResult TryFindPath(
            in NavigationPathQuery query,
            Span<NavigationNodeId> destination,
            out int writtenCount)
        {
            if (_status == NavigationQueryStatus.CapacityExceeded)
            {
                writtenCount = destination.Length + 1;
                return NavigationQueryResult.Failure(NavigationQueryStatus.CapacityExceeded);
            }

            if (_status != NavigationQueryStatus.Success && _status != NavigationQueryStatus.PartialPath)
            {
                writtenCount = 0;
                return NavigationQueryResult.Failure(_status);
            }

            int length = _pathLength;
            if (length > destination.Length)
            {
                writtenCount = 0;
                return NavigationQueryResult.Failure(NavigationQueryStatus.CapacityExceeded);
            }

            for (int i = 0; i < length; i++)
            {
                destination[i] = NavigationNodeId.FromInt32(i);
            }

            writtenCount = length;
            return new NavigationQueryResult(_status, length, 0);
        }
    }

    private sealed class RecordingFailureHook : INavigationFailureMemoryHook
    {
        public int FailureCount { get; private set; }

        public NavigationQueryStatus LastStatus { get; private set; }

        public MemoryWriteResult RecordPathFailure(AgentId agent, NavigationQueryStatus status, long tickSequence)
        {
            FailureCount++;
            LastStatus = status;
            return MemoryWriteResult.Success(MemoryRecordId.FromInt32(0), 0);
        }
    }

    private sealed class DoorLinkClassifier : INavigationTraversalClassifier
    {
        public NavigationQueryFlags GetRequiredFlags(NavigationNodeId from, NavigationNodeId destination)
        {
            return NavigationQueryFlags.AllowDoorLinks;
        }
    }
}
