using DynamicPlanningAI.Abstractions.Enums;

namespace DynamicPlanningAI.Runtime.Memory;

/// <summary>
/// Deterministic eviction priority helpers for working-memory records.
/// </summary>
internal static class MemoryPriority
{
    /// <summary>
    /// Computes an eviction priority; higher values are retained longer.
    /// </summary>
    /// <param name="type">Memory type.</param>
    /// <param name="flags">Record flags.</param>
    /// <returns>Non-negative priority score.</returns>
    public static int Compute(MemoryType type, MemoryRecordFlags flags)
    {
        if ((flags & MemoryRecordFlags.ImmediateDanger) != 0
            || type == MemoryType.DangerDetected
            || type == MemoryType.GrenadeDetected)
        {
            return 1000;
        }

        if ((flags & MemoryRecordFlags.Ambient) != 0)
        {
            return 10;
        }

        return type switch
        {
            MemoryType.DamageReceived => 800,
            MemoryType.TargetSeen => 700,
            MemoryType.SquadOrderReceived => 650,
            MemoryType.CommunicationReceived => 600,
            MemoryType.TargetHeard => 500,
            MemoryType.TargetLost => 400,
            MemoryType.CoverInvalid => 350,
            MemoryType.CoverReservationLost => 350,
            MemoryType.NavigationFailed => 300,
            MemoryType.TraversalFailed => 300,
            MemoryType.DoorBlocked => 250,
            MemoryType.SearchSectorInspected => 50,
            _ => 100,
        };
    }

    /// <summary>
    /// Returns whether a candidate write is ambient (must not displace immediate danger).
    /// </summary>
    /// <param name="type">Candidate type.</param>
    /// <param name="flags">Candidate flags.</param>
    /// <returns><see langword="true"/> when the write is ambient priority.</returns>
    public static bool IsAmbient(MemoryType type, MemoryRecordFlags flags)
    {
        if ((flags & MemoryRecordFlags.Ambient) != 0)
        {
            return true;
        }

        return type == MemoryType.SearchSectorInspected;
    }

    /// <summary>
    /// Returns whether a live record is immediate danger.
    /// </summary>
    /// <param name="type">Record type.</param>
    /// <param name="flags">Record flags.</param>
    /// <returns><see langword="true"/> when the record is protected danger.</returns>
    public static bool IsImmediateDanger(MemoryType type, MemoryRecordFlags flags)
    {
        return (flags & MemoryRecordFlags.ImmediateDanger) != 0
            || type == MemoryType.DangerDetected
            || type == MemoryType.GrenadeDetected;
    }
}
