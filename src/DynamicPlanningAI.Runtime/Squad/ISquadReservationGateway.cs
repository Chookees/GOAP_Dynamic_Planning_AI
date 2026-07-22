using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Adapter-friendly gateway for squad resource reservations.
/// </summary>
/// <remarks>
/// Hosts or the Reservations subsystem implement this interface. The squad
/// coordinator never reaches into reservation tables directly, keeping the
/// dependency invertible until Reservations is wired.
/// </remarks>
public interface ISquadReservationGateway
{
    /// <summary>
    /// Attempts to reserve a resource for an agent.
    /// </summary>
    /// <param name="agent">Requesting agent.</param>
    /// <param name="resourceId">Raw resource identifier (e.g. tactical point value).</param>
    /// <param name="tickSequence">Current tick sequence.</param>
    /// <returns>Reservation outcome.</returns>
    public ReservationResult TryReserve(AgentId agent, int resourceId, long tickSequence);

    /// <summary>
    /// Releases a previously acquired reservation.
    /// </summary>
    /// <param name="agent">Owning agent.</param>
    /// <param name="resourceId">Raw resource identifier.</param>
    /// <returns>Release outcome.</returns>
    public ReservationResult Release(AgentId agent, int resourceId);
}
