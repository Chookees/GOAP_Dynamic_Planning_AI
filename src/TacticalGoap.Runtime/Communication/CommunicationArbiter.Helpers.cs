using TacticalGoap.Abstractions.Attributes;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;

namespace TacticalGoap.Runtime.Communication;

public sealed partial class CommunicationArbiter
{
    [FrozenRuntimePath]
    private int FindFreePendingSlot()
    {
        for (int i = 0; i < _pendingCount && i < _options.MaxPendingRequests; i++)
        {
            if (!_pendingLive[i])
            {
                return i;
            }
        }

        if (_pendingCount < _options.MaxPendingRequests)
        {
            return _pendingCount;
        }

        return -1;
    }

    [FrozenRuntimePath]
    private void ExpirePending(long tick)
    {
        for (int i = 0; i < _pendingCount && i < _options.MaxPendingRequests; i++)
        {
            if (!_pendingLive[i])
            {
                continue;
            }

            if (tick > _pending[i].ExpiryTick)
            {
                _pendingLive[i] = false;
            }
        }
    }

    [FrozenRuntimePath]
    private int BuildCandidateOrder(long tick)
    {
        int count = 0;
        for (int i = 0; i < _pendingCount && i < _options.MaxPendingRequests; i++)
        {
            if (!_pendingLive[i])
            {
                continue;
            }

            CommunicationRequest request = _pending[i];
            if (!request.IsStructurallyValid || tick > request.ExpiryTick)
            {
                _pendingLive[i] = false;
                continue;
            }

            _scoreScratch[i] = ComputeScore(in request, tick);
            _orderScratch[count] = i;
            count = checked(count + 1);
        }

        return count;
    }

    [FrozenRuntimePath]
    private static int ComputeScore(in CommunicationRequest request, long tick)
    {
        int basePriority = GetBasePriority(request.Intent);
        int priority = checked(basePriority + request.Priority);
        long age = checked(tick - request.SubmittedTick);
        if (age < 0)
        {
            age = 0;
        }

        // Recency bonus: fresher requests score slightly higher (bounded).
        int recency = age > 32 ? 0 : checked(32 - (int)age);
        return checked(priority + recency);
    }

    [FrozenRuntimePath]
    private void SortCandidatesDeterministic(int candidateCount)
    {
        // Insertion sort: score desc, then intent id asc.
        for (int i = 1; i < candidateCount; i++)
        {
            int keyIndex = _orderScratch[i];
            int keyScore = _scoreScratch[keyIndex];
            int keyId = _pending[keyIndex].IntentId.Value;
            int j = i - 1;
            while (j >= 0)
            {
                int otherIndex = _orderScratch[j];
                int otherScore = _scoreScratch[otherIndex];
                int otherId = _pending[otherIndex].IntentId.Value;
                bool otherWins = otherScore > keyScore || (otherScore == keyScore && otherId < keyId);
                if (otherWins)
                {
                    break;
                }

                _orderScratch[j + 1] = _orderScratch[j];
                j = checked(j - 1);
            }

            _orderScratch[j + 1] = keyIndex;
        }
    }

    [FrozenRuntimePath]
    private bool PassesRuntimeGates(in CommunicationRequest request, long tick)
    {
        if (!request.Speaker.IsValid || request.Speaker.Value >= AiHardLimits.MaximumAgents)
        {
            return false;
        }

        long speakerLast = _speakerLastEmit[request.Speaker.Value];
        if (checked(tick - speakerLast) < _options.SpeakerCooldownTicks)
        {
            return false;
        }

        if (request.Squad.IsValid && request.Squad.Value < AiHardLimits.MaximumSquads)
        {
            long squadLast = _squadLastEmit[request.Squad.Value];
            if (checked(tick - squadLast) < _options.SquadChannelCooldownTicks)
            {
                return false;
            }
        }

        if (IsDuplicateSuppressed(in request, tick))
        {
            return false;
        }

        return true;
    }

    [FrozenRuntimePath]
    private bool IsDuplicateSuppressed(in CommunicationRequest request, long tick)
    {
        for (int i = 0; i < _recentCount && i < _options.MaxPendingRequests; i++)
        {
            if (checked(tick - _recentTick[i]) > _options.DuplicateSuppressionTicks)
            {
                continue;
            }

            CommunicationEmission recent = _recent[i];
            if (recent.Speaker != request.Speaker)
            {
                continue;
            }

            if (recent.Intent != request.Intent)
            {
                continue;
            }

            if (recent.RelatedEntity != request.RelatedEntity)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    [FrozenRuntimePath]
    private void RecordEmission(in CommunicationEmission emission)
    {
        if (emission.Speaker.IsValid && emission.Speaker.Value < AiHardLimits.MaximumAgents)
        {
            _speakerLastEmit[emission.Speaker.Value] = emission.EmittedTick;
        }

        if (emission.Squad.IsValid && emission.Squad.Value < AiHardLimits.MaximumSquads)
        {
            _squadLastEmit[emission.Squad.Value] = emission.EmittedTick;
        }

        int slot;
        if (_recentCount < _options.MaxPendingRequests)
        {
            slot = _recentCount;
            _recentCount = checked(_recentCount + 1);
        }
        else
        {
            // Overwrite oldest (index 0) by shifting — bounded, deterministic.
            for (int i = 1; i < _recentCount; i++)
            {
                _recent[i - 1] = _recent[i];
                _recentTick[i - 1] = _recentTick[i];
            }

            slot = checked(_recentCount - 1);
        }

        _recent[slot] = emission;
        _recentTick[slot] = emission.EmittedTick;
    }

    [FrozenRuntimePath]
    private void CompactPending()
    {
        int write = 0;
        for (int read = 0; read < _pendingCount && read < _options.MaxPendingRequests; read++)
        {
            if (!_pendingLive[read])
            {
                continue;
            }

            if (write != read)
            {
                _pending[write] = _pending[read];
                _pendingLive[write] = true;
                _pendingLive[read] = false;
            }

            write = checked(write + 1);
        }

        _pendingCount = write;
    }
}
