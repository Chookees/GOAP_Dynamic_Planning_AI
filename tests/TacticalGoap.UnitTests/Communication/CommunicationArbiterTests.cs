using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Abstractions.Ticks;
using TacticalGoap.Runtime.Communication;
using Xunit;

namespace TacticalGoap.UnitTests.Communication;

public sealed class CommunicationArbiterTests
{
    [Fact]
    public void Priority_EmitsHigherPriorityFirst()
    {
        CommunicationArbiter arbiter = CreateArbiter(maxConcurrent: 1);
        arbiter.SynchronizeTick(1);
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(0), CommunicationIntentType.HoldingPosition, EntityId.Invalid));
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(1), CommunicationIntentType.GrenadeWarning, EntityId.Invalid));

        CommunicationEmission[] buffer = new CommunicationEmission[4];
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(1, 16), buffer, out int count));
        Assert.Equal(1, count);
        Assert.Equal(CommunicationIntentType.GrenadeWarning, buffer[0].Intent);
    }

    [Fact]
    public void SpeakerCooldown_SuppressesSecondEmission()
    {
        CommunicationArbiterOptions options = new()
        {
            MaxConcurrentEmissions = 2,
            SpeakerCooldownTicks = 5,
            SquadChannelCooldownTicks = 0,
            DuplicateSuppressionTicks = 0,
        };
        CommunicationArbiter arbiter = new(options);
        arbiter.SynchronizeTick(1);
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(0), CommunicationIntentType.TakingFire, EntityId.Invalid));
        CommunicationEmission[] buffer = new CommunicationEmission[4];
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(1, 16), buffer, out int count));
        Assert.Equal(1, count);

        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(0), CommunicationIntentType.Reloading, EntityId.Invalid));
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(2, 16), buffer, out count));
        Assert.Equal(0, count);

        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(7, 16), buffer, out count));
        Assert.Equal(1, count);
        Assert.Equal(CommunicationIntentType.Reloading, buffer[0].Intent);
    }

    [Fact]
    public void DuplicateSuppression_BlocksSameIntent()
    {
        CommunicationArbiterOptions options = new()
        {
            MaxConcurrentEmissions = 2,
            SpeakerCooldownTicks = 0,
            SquadChannelCooldownTicks = 0,
            DuplicateSuppressionTicks = 10,
        };
        CommunicationArbiter arbiter = new(options);
        EntityId target = EntityId.FromInt32(9);
        arbiter.SynchronizeTick(1);
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(0), CommunicationIntentType.ContactSpotted, target));
        CommunicationEmission[] buffer = new CommunicationEmission[4];
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(1, 16), buffer, out int count));
        Assert.Equal(1, count);

        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(0), CommunicationIntentType.ContactSpotted, target));
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(2, 16), buffer, out count));
        Assert.Equal(0, count);
    }

    [Fact]
    public void Expiration_DropsStaleRequests()
    {
        CommunicationArbiter arbiter = CreateArbiter(maxConcurrent: 2);
        arbiter.SynchronizeTick(1);
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(
                AgentId.FromInt32(0),
                CommunicationIntentType.Advancing,
                EntityId.Invalid,
                SquadId.Invalid,
                priority: 0,
                expiryTick: 3));

        CommunicationEmission[] buffer = new CommunicationEmission[4];
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(4, 16), buffer, out int count));
        Assert.Equal(0, count);
        Assert.Equal(0, arbiter.PendingCount);
    }

    [Fact]
    public void DeterministicOrder_TieBreaksByIntentId()
    {
        CommunicationArbiterOptions options = new()
        {
            MaxConcurrentEmissions = 3,
            SpeakerCooldownTicks = 0,
            SquadChannelCooldownTicks = 0,
            DuplicateSuppressionTicks = 0,
        };
        CommunicationArbiter arbiter = new(options);
        arbiter.SynchronizeTick(1);

        // Same type => same base priority; emission order follows intent id ascending.
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(2), CommunicationIntentType.Reloading, EntityId.Invalid));
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(0), CommunicationIntentType.Reloading, EntityId.Invalid));
        Assert.Equal(
            OperationStatus.Success,
            arbiter.Request(AgentId.FromInt32(1), CommunicationIntentType.Reloading, EntityId.Invalid));

        CommunicationEmission[] buffer = new CommunicationEmission[4];
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(1, 16), buffer, out int count));
        Assert.Equal(3, count);
        Assert.True(buffer[0].IntentId.Value < buffer[1].IntentId.Value);
        Assert.True(buffer[1].IntentId.Value < buffer[2].IntentId.Value);
    }

    [Fact]
    public void AllIntentTypes_CanBeRequested()
    {
        CommunicationArbiter arbiter = CreateArbiter(maxConcurrent: 32);
        arbiter.SynchronizeTick(1);
        for (int i = 0; i <= (int)CommunicationIntentType.OrderFailed; i++)
        {
            OperationStatus status = arbiter.Request(
                AgentId.FromInt32(i % 8),
                (CommunicationIntentType)i,
                EntityId.Invalid,
                SquadId.FromInt32(i % 4),
                priority: 0,
                expiryTick: 100);
            Assert.Equal(OperationStatus.Success, status);
        }

        CommunicationEmission[] buffer = new CommunicationEmission[32];
        Assert.Equal(OperationStatus.Success, arbiter.Tick(new AiTick(1, 16), buffer, out int count));
        Assert.True(count > 0);
    }

    private static CommunicationArbiter CreateArbiter(int maxConcurrent)
    {
        return new CommunicationArbiter(new CommunicationArbiterOptions
        {
            MaxConcurrentEmissions = maxConcurrent,
            SpeakerCooldownTicks = 0,
            SquadChannelCooldownTicks = 0,
            DuplicateSuppressionTicks = 0,
        });
    }
}
