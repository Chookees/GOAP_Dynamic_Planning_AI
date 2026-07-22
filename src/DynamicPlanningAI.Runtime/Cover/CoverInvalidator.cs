using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Enums;

namespace DynamicPlanningAI.Runtime.Cover;

/// <summary>
/// Determines whether a cover candidate is invalidated for the current tick.
/// </summary>
/// <remarks>
/// Invalidation is evaluated from caller-supplied sample facts. The invalidator
/// does not scan the map or allocate.
/// </remarks>
public static class CoverInvalidator
{
    /// <summary>
    /// Evaluates invalidation reasons for a candidate sample.
    /// </summary>
    /// <param name="sample">Candidate with dynamic state.</param>
    /// <param name="agentStance">Current agent stance.</param>
    /// <param name="maxExposure">Inclusive exposure threshold before invalidation.</param>
    /// <param name="reservationLost">Whether a previously held reservation was lost.</param>
    /// <returns>First matching invalidation reason, or <see cref="CoverInvalidationReason.None"/>.</returns>
    [FrozenRuntimePath]
    public static CoverInvalidationReason Evaluate(
        in CoverCandidateSample sample,
        AgentStance agentStance,
        int maxExposure,
        bool reservationLost)
    {
        if (sample.IsDestroyed)
        {
            return CoverInvalidationReason.Destroyed;
        }

        if (!sample.IsNavigationAvailable)
        {
            return CoverInvalidationReason.NavigationUnavailable;
        }

        if (sample.InGrenadeDanger)
        {
            return CoverInvalidationReason.GrenadeRegion;
        }

        if (reservationLost)
        {
            return CoverInvalidationReason.ReservationLost;
        }

        if (sample.Point.Exposure > maxExposure)
        {
            return CoverInvalidationReason.ThreatExposure;
        }

        if (sample.Point.MaxOccupants > 0 && sample.OccupantCount >= sample.Point.MaxOccupants)
        {
            return CoverInvalidationReason.OverCapacity;
        }

        if (!IsStanceSupported(sample.Point.PreferredStance, sample.Point.CoverHeight, agentStance))
        {
            return CoverInvalidationReason.StanceUnsupported;
        }

        return CoverInvalidationReason.None;
    }

    /// <summary>
    /// Applies <see cref="Evaluate"/> and returns a sample with the reason stamped.
    /// </summary>
    /// <param name="sample">Candidate with dynamic state.</param>
    /// <param name="agentStance">Current agent stance.</param>
    /// <param name="maxExposure">Inclusive exposure threshold before invalidation.</param>
    /// <param name="reservationLost">Whether a previously held reservation was lost.</param>
    /// <returns>A copy of the sample with <see cref="CoverCandidateSample.InvalidationReason"/> set.</returns>
    [FrozenRuntimePath]
    public static CoverCandidateSample Apply(
        in CoverCandidateSample sample,
        AgentStance agentStance,
        int maxExposure,
        bool reservationLost)
    {
        CoverInvalidationReason reason = Evaluate(sample, agentStance, maxExposure, reservationLost);
        return new CoverCandidateSample(
            sample.Point,
            sample.PathCost,
            sample.OccupantCount,
            sample.IsReservedByOther,
            sample.ReservationHeld,
            sample.HasLineOfFire,
            sample.InGrenadeDanger,
            sample.IsDestroyed,
            sample.IsNavigationAvailable,
            sample.AllySeparation,
            sample.OrderCompatible,
            reason);
    }

    [FrozenRuntimePath]
    private static bool IsStanceSupported(AgentStance preferred, CoverHeight height, AgentStance agentStance)
    {
        if (preferred == agentStance || preferred == AgentStance.InCover)
        {
            return true;
        }

        if (height == CoverHeight.None)
        {
            return agentStance == AgentStance.Standing;
        }

        if (height == CoverHeight.Low)
        {
            return agentStance == AgentStance.Crouching || agentStance == AgentStance.Prone;
        }

        return agentStance == AgentStance.Standing || agentStance == AgentStance.Crouching;
    }
}
