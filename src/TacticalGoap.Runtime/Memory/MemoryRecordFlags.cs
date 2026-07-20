using System;
using System.Diagnostics.CodeAnalysis;

namespace TacticalGoap.Runtime.Memory;

/// <summary>
/// Bit flags attached to a working-memory record.
/// </summary>
[Flags]
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Flags suffix is conventional for [Flags] enumerations.")]
public enum MemoryRecordFlags : int
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>Record represents immediate lethal danger; protected from ambient eviction.</summary>
    ImmediateDanger = 1 << 0,

    /// <summary>Low-priority ambient evidence (search, routine observations).</summary>
    Ambient = 1 << 1,

    /// <summary>Source entity identity is uncertain.</summary>
    UncertainSource = 1 << 2,

    /// <summary>Position is approximate or unknown; not an omniscient host truth.</summary>
    UncertainPosition = 1 << 3,

    /// <summary>Record has current visual confirmation.</summary>
    Visible = 1 << 4,

    /// <summary>Squad designated this contact as focus.</summary>
    SquadDesignated = 1 << 5,

    /// <summary>Entity attributed as a damage dealer.</summary>
    DamageAttributed = 1 << 6,
}
