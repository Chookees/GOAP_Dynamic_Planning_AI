using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Runtime.Memory;
using DynamicPlanningAI.Runtime.Selection;
using Xunit;

namespace DynamicPlanningAI.UnitTests.Selection;

public sealed class SelectorTests
{
    [Fact]
    public void TargetSelector_TieBreaksByAscendingEntityId()
    {
        WorkingMemoryStore memory = new(8);
        InsertSeen(memory, EntityId.FromInt32(5), confidence: 500, tick: 10L);
        InsertSeen(memory, EntityId.FromInt32(2), confidence: 500, tick: 10L);

        TargetSelector selector = new TargetSelector();
        TargetSelectionRequest request = new TargetSelectionRequest
        {
            Agent = AgentId.FromInt32(0),
            AgentPosition = Int2.Zero,
            CurrentTick = 10L,
            CurrentFocus = EntityId.Invalid,
            HysteresisBonus = 0,
            MaxCandidates = 8,
        };

        TargetSelectionResult result = selector.Select(memory, request, spatial: null, trace: null);
        Assert.Equal(EntityId.FromInt32(2), result.Selected);
    }

    [Fact]
    public void TargetSelector_AppliesHysteresis()
    {
        WorkingMemoryStore memory = new(8);
        InsertSeen(memory, EntityId.FromInt32(1), confidence: 500, tick: 10L);
        InsertSeen(memory, EntityId.FromInt32(2), confidence: 520, tick: 10L);

        TargetSelector selector = new TargetSelector();
        TargetSelectionRequest request = new TargetSelectionRequest
        {
            Agent = AgentId.FromInt32(0),
            AgentPosition = Int2.Zero,
            CurrentTick = 10L,
            CurrentFocus = EntityId.FromInt32(1),
            HysteresisBonus = 100,
            MaxCandidates = 8,
        };

        TargetSelectionResult result = selector.Select(memory, request, spatial: null, trace: null);
        Assert.Equal(EntityId.FromInt32(1), result.Selected);
        Assert.False(result.Changed);
    }

    [Fact]
    public void TargetSelector_IgnoresUnseenEntities()
    {
        WorkingMemoryStore memory = new(8);
        memory.InsertOrUpdate(new MemoryRecord
        {
            Type = MemoryType.SearchSectorInspected,
            RelatedEntity = EntityId.FromInt32(9),
            SourceEntity = EntityId.FromInt32(0),
            CreationTick = 1L,
            UpdateTick = 1L,
            ExpirationTick = -1L,
            Confidence = 1000,
            Flags = MemoryRecordFlags.Ambient,
        });

        TargetSelector selector = new TargetSelector();
        TargetSelectionRequest request = new TargetSelectionRequest
        {
            Agent = AgentId.FromInt32(0),
            CurrentTick = 1L,
            CurrentFocus = EntityId.Invalid,
            MaxCandidates = 8,
        };

        TargetSelectionResult result = selector.Select(memory, request, spatial: null, trace: null);
        Assert.False(result.Selected.IsValid);
    }

    [Fact]
    public void WeaponSelector_PrefersLoadedInRangeWeapon_WithHysteresis()
    {
        WeaponCandidate[] candidates =
        [
            new WeaponCandidate
            {
                Weapon = WeaponId.FromInt32(1),
                Ammunition = 30,
                RequiresReload = false,
                PreferredRange = DistanceCategory.Medium,
                SwitchCost = 40,
            },
            new WeaponCandidate
            {
                Weapon = WeaponId.FromInt32(2),
                Ammunition = 30,
                RequiresReload = false,
                PreferredRange = DistanceCategory.Medium,
                SwitchCost = 40,
            },
        ];

        WeaponSelector selector = new WeaponSelector();
        WeaponSelectionRequest request = new WeaponSelectionRequest
        {
            Agent = AgentId.FromInt32(0),
            CurrentTick = 1L,
            TargetRange = DistanceCategory.Medium,
            CurrentWeapon = WeaponId.FromInt32(2),
            HysteresisBonus = 50,
        };

        WeaponSelectionResult result = selector.Select(candidates, request, trace: null);
        Assert.Equal(WeaponId.FromInt32(2), result.Selected);
        Assert.False(result.Changed);
    }

    [Fact]
    public void WeaponSelector_RejectsEmptyMagazineRelativeToLoaded()
    {
        WeaponCandidate[] candidates =
        [
            new WeaponCandidate
            {
                Weapon = WeaponId.FromInt32(1),
                Ammunition = 0,
                RequiresReload = true,
                PreferredRange = DistanceCategory.Near,
            },
            new WeaponCandidate
            {
                Weapon = WeaponId.FromInt32(2),
                Ammunition = 10,
                RequiresReload = false,
                PreferredRange = DistanceCategory.Near,
            },
        ];

        WeaponSelector selector = new WeaponSelector();
        WeaponSelectionRequest request = new WeaponSelectionRequest
        {
            Agent = AgentId.FromInt32(0),
            CurrentTick = 1L,
            TargetRange = DistanceCategory.Near,
            CurrentWeapon = WeaponId.Invalid,
        };

        WeaponSelectionResult result = selector.Select(candidates, request, trace: null);
        Assert.Equal(WeaponId.FromInt32(2), result.Selected);
    }

    private static void InsertSeen(WorkingMemoryStore memory, EntityId entity, int confidence, long tick)
    {
        memory.InsertOrUpdate(new MemoryRecord
        {
            Type = MemoryType.TargetSeen,
            RelatedEntity = entity,
            SourceEntity = EntityId.FromInt32(0),
            Position = new Int2(1, 0),
            CreationTick = tick,
            UpdateTick = tick,
            ExpirationTick = -1L,
            Confidence = confidence,
            Flags = MemoryRecordFlags.Visible,
        });
    }
}
