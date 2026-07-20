using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Squad;

/// <summary>
/// Fixed-capacity in-memory reservation gateway for tests and early wiring.
/// </summary>
public sealed class LocalSquadReservationGateway : ISquadReservationGateway
{
    private readonly int[] _resourceIds;
    private readonly AgentId[] _owners;
    private int _count;

    /// <summary>
    /// Initializes a local gateway with hard-limit capacity.
    /// </summary>
    public LocalSquadReservationGateway()
    {
        _resourceIds = new int[AiHardLimits.MaximumReservations];
        _owners = new AgentId[AiHardLimits.MaximumReservations];
        _count = 0;
        for (int i = 0; i < _owners.Length; i++)
        {
            _owners[i] = AgentId.Invalid;
            _resourceIds[i] = -1;
        }
    }

    /// <inheritdoc />
    public ReservationResult TryReserve(AgentId agent, int resourceId, long tickSequence)
    {
        _ = tickSequence;
        if (!agent.IsValid || resourceId < 0)
        {
            return ReservationResult.Failure(ReservationStatus.InvalidRequest, AgentId.Invalid, resourceId);
        }

        for (int i = 0; i < _count && i < AiHardLimits.MaximumReservations; i++)
        {
            if (_resourceIds[i] != resourceId)
            {
                continue;
            }

            if (_owners[i] == agent)
            {
                return ReservationResult.Acquired(i, agent, resourceId);
            }

            return ReservationResult.Failure(ReservationStatus.Conflict, _owners[i], resourceId);
        }

        if (_count >= AiHardLimits.MaximumReservations)
        {
            return ReservationResult.Failure(ReservationStatus.CapacityExceeded, AgentId.Invalid, resourceId);
        }

        int slot = _count;
        _resourceIds[slot] = resourceId;
        _owners[slot] = agent;
        _count = checked(_count + 1);
        return ReservationResult.Acquired(slot, agent, resourceId);
    }

    /// <inheritdoc />
    public ReservationResult Release(AgentId agent, int resourceId)
    {
        for (int i = 0; i < _count && i < AiHardLimits.MaximumReservations; i++)
        {
            if (_resourceIds[i] != resourceId || _owners[i] != agent)
            {
                continue;
            }

            int last = checked(_count - 1);
            _resourceIds[i] = _resourceIds[last];
            _owners[i] = _owners[last];
            _resourceIds[last] = -1;
            _owners[last] = AgentId.Invalid;
            _count = last;
            return new ReservationResult(ReservationStatus.Released, i, agent, resourceId);
        }

        return ReservationResult.Failure(ReservationStatus.NotHeld, AgentId.Invalid, resourceId);
    }
}
