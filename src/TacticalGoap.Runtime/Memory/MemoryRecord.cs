using TacticalGoap.Abstractions.Enums;
using TacticalGoap.Abstractions.Geometry;
using TacticalGoap.Abstractions.Identifiers;

namespace TacticalGoap.Runtime.Memory;

/// <summary>
/// Blittable working-memory record stored in a fixed-capacity agent table.
/// </summary>
/// <remarks>
/// Records are value types with no managed references. Payload fields are plain
/// integers for host-defined secondary data.
/// </remarks>
public struct MemoryRecord
{
    /// <summary>
    /// Gets or sets the stable record identifier (slot index while live).
    /// </summary>
    public MemoryRecordId Id { get; set; }

    /// <summary>
    /// Gets or sets the memory type.
    /// </summary>
    public MemoryType Type { get; set; }

    /// <summary>
    /// Gets or sets the source entity that produced the evidence.
    /// </summary>
    public EntityId SourceEntity { get; set; }

    /// <summary>
    /// Gets or sets the related subject entity (e.g. observed target).
    /// </summary>
    public EntityId RelatedEntity { get; set; }

    /// <summary>
    /// Gets or sets the believed cell position.
    /// </summary>
    public Int2 Position { get; set; }

    /// <summary>
    /// Gets or sets an optional navigation node reference.
    /// </summary>
    public NavigationNodeId NavigationNode { get; set; }

    /// <summary>
    /// Gets or sets the tick sequence when the record was created.
    /// </summary>
    public long CreationTick { get; set; }

    /// <summary>
    /// Gets or sets the tick sequence of the most recent update.
    /// </summary>
    public long UpdateTick { get; set; }

    /// <summary>
    /// Gets or sets the exclusive expiration tick sequence; negative means never.
    /// </summary>
    public long ExpirationTick { get; set; }

    /// <summary>
    /// Gets or sets confidence in the inclusive range 0..1000.
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// Gets or sets record flags.
    /// </summary>
    public MemoryRecordFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets the first integer payload.
    /// </summary>
    public int Payload0 { get; set; }

    /// <summary>
    /// Gets or sets the second integer payload.
    /// </summary>
    public int Payload1 { get; set; }

    /// <summary>
    /// Gets or sets the third integer payload.
    /// </summary>
    public int Payload2 { get; set; }

    /// <summary>
    /// Gets or sets the fourth integer payload.
    /// </summary>
    public int Payload3 { get; set; }

    /// <summary>
    /// Gets a value indicating whether this slot holds a live record.
    /// </summary>
    public bool IsOccupied => Id.IsValid;

    /// <summary>
    /// Clears the slot to an empty state.
    /// </summary>
    public void Clear()
    {
        Id = MemoryRecordId.Invalid;
        Type = MemoryType.TargetSeen;
        SourceEntity = EntityId.Invalid;
        RelatedEntity = EntityId.Invalid;
        Position = Int2.Zero;
        NavigationNode = NavigationNodeId.Invalid;
        CreationTick = 0L;
        UpdateTick = 0L;
        ExpirationTick = -1L;
        Confidence = 0;
        Flags = MemoryRecordFlags.None;
        Payload0 = 0;
        Payload1 = 0;
        Payload2 = 0;
        Payload3 = 0;
    }
}
