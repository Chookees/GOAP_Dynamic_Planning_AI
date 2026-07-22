using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;

namespace DynamicPlanningAI.Runtime.Squad;

/// <summary>
/// Fixed membership slot within a squad.
/// </summary>
public struct SquadMemberSlot
{
    /// <summary>
    /// Initializes an empty slot.
    /// </summary>
    /// <param name="slotIndex">Zero-based slot index within the squad.</param>
    public SquadMemberSlot(byte slotIndex)
    {
        SlotIndex = slotIndex;
        AgentId = AgentId.Invalid;
        Role = SquadSlotRole.None;
        ActiveOrderId = OrderId.Invalid;
        FormationOffset = Int2.Zero;
        IsOccupied = false;
        Score = 0;
    }

    /// <summary>
    /// Gets or sets the zero-based slot index within the squad.
    /// </summary>
    public byte SlotIndex { get; set; }

    /// <summary>
    /// Gets or sets the occupying agent; invalid when empty.
    /// </summary>
    public AgentId AgentId { get; set; }

    /// <summary>
    /// Gets or sets the behavior role for this slot.
    /// </summary>
    public SquadSlotRole Role { get; set; }

    /// <summary>
    /// Gets or sets the active order assigned to this slot.
    /// </summary>
    public OrderId ActiveOrderId { get; set; }

    /// <summary>
    /// Gets or sets the formation offset relative to squad focus.
    /// </summary>
    public Int2 FormationOffset { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the slot is occupied.
    /// </summary>
    public bool IsOccupied { get; set; }

    /// <summary>
    /// Gets or sets the last assignment score used for deterministic selection.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Clears occupancy and role state while preserving the slot index.
    /// </summary>
    public void Clear()
    {
        AgentId = AgentId.Invalid;
        Role = SquadSlotRole.None;
        ActiveOrderId = OrderId.Invalid;
        FormationOffset = Int2.Zero;
        IsOccupied = false;
        Score = 0;
    }
}
