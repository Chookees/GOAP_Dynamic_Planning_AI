using System;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Runtime.WorldState;

/// <summary>
/// Fixed-capacity symbolic world state with a 64-fact bit mask and value table.
/// </summary>
/// <remarks>
/// Capacity is fixed at construction (<see cref="AiHardLimits.MaximumWorldFacts"/>).
/// No managed allocations occur after the constructor. Hashing is deterministic
/// FNV-1a over the specified mask and ordered fact values.
/// </remarks>
public sealed class SymbolicWorldState
{
    private const ulong FnvOffset = 2166136261UL;
    private const ulong FnvPrime = 16777619UL;

    private readonly int[] _values;
    private readonly int _capacity;

    /// <summary>
    /// Initializes an empty symbolic world state with hard-limit capacity.
    /// </summary>
    public SymbolicWorldState()
        : this(AiHardLimits.MaximumWorldFacts)
    {
    }

    /// <summary>
    /// Initializes an empty symbolic world state with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Fact capacity in the inclusive range 1..64.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is invalid.</exception>
    public SymbolicWorldState(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumWorldFacts)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be in 1..64.");
        }

        _capacity = capacity;
        _values = new int[capacity];
        SpecifiedMask = 0UL;
    }

    /// <summary>
    /// Gets the bit mask of currently specified facts.
    /// </summary>
    public ulong SpecifiedMask { get; private set; }

    /// <summary>
    /// Gets the fixed fact capacity.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Sets a fact to a concrete value.
    /// </summary>
    /// <param name="factId">Fact identifier.</param>
    /// <param name="value">Fact value.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus Set(WorldFactId factId, int value)
    {
        int index = (int)factId;
        if (!IsValidIndex(index))
        {
            return OperationStatus.InvalidArgument;
        }

        _values[index] = value;
        SpecifiedMask |= 1UL << index;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Clears a fact so it becomes unspecified.
    /// </summary>
    /// <param name="factId">Fact identifier.</param>
    /// <returns>Success, no-op, or validation failure.</returns>
    public OperationStatus Clear(WorldFactId factId)
    {
        int index = (int)factId;
        if (!IsValidIndex(index))
        {
            return OperationStatus.InvalidArgument;
        }

        ulong bit = 1UL << index;
        if ((SpecifiedMask & bit) == 0UL)
        {
            return OperationStatus.NoOp;
        }

        SpecifiedMask &= ~bit;
        _values[index] = 0;
        return OperationStatus.Success;
    }

    /// <summary>
    /// Attempts to read a specified fact value.
    /// </summary>
    /// <param name="factId">Fact identifier.</param>
    /// <param name="value">Receives the value when specified.</param>
    /// <returns><see langword="true"/> when the fact is specified.</returns>
    public bool TryGet(WorldFactId factId, out int value)
    {
        int index = (int)factId;
        if (!IsValidIndex(index))
        {
            value = 0;
            return false;
        }

        ulong bit = 1UL << index;
        if ((SpecifiedMask & bit) == 0UL)
        {
            value = 0;
            return false;
        }

        value = _values[index];
        return true;
    }

    /// <summary>
    /// Returns whether a fact is currently specified.
    /// </summary>
    /// <param name="factId">Fact identifier.</param>
    /// <returns><see langword="true"/> when specified.</returns>
    public bool Contains(WorldFactId factId)
    {
        int index = (int)factId;
        if (!IsValidIndex(index))
        {
            return false;
        }

        return (SpecifiedMask & (1UL << index)) != 0UL;
    }

    /// <summary>
    /// Returns whether this state satisfies every required fact in <paramref name="requirements"/>.
    /// </summary>
    /// <param name="requirements">Required facts and values.</param>
    /// <returns><see langword="true"/> when all requirements are met.</returns>
    public bool Satisfies(SymbolicWorldState requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        ulong remaining = requirements.SpecifiedMask;
        while (remaining != 0UL)
        {
            int index = TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;

            if ((SpecifiedMask & bit) == 0UL)
            {
                return false;
            }

            if (_values[index] != requirements._values[index])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns whether this state conflicts with <paramref name="other"/> on any shared fact.
    /// </summary>
    /// <param name="other">Other state.</param>
    /// <returns><see langword="true"/> when a shared fact has unequal values.</returns>
    public bool ConflictsWith(SymbolicWorldState other)
    {
        ArgumentNullException.ThrowIfNull(other);

        ulong shared = SpecifiedMask & other.SpecifiedMask;
        while (shared != 0UL)
        {
            int index = TrailingZeroCount(shared);
            ulong bit = 1UL << index;
            shared &= ~bit;

            if (_values[index] != other._values[index])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies a bounded list of set/clear effects in order.
    /// </summary>
    /// <param name="effects">Effect buffer.</param>
    /// <param name="count">Number of valid effects.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus ApplyEffects(ReadOnlySpan<WorldStateEffect> effects, int count)
    {
        if (count < 0 || count > effects.Length)
        {
            return OperationStatus.InvalidArgument;
        }

        for (int i = 0; i < count; i++)
        {
            OperationStatus status = ApplyOneEffect(effects[i]);
            if (status != OperationStatus.Success && status != OperationStatus.NoOp)
            {
                return status;
            }
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Regresses this requirement state through an action's effects and preconditions.
    /// </summary>
    /// <remarks>
    /// Satisfied requirements established by set-effects are removed. Preconditions
    /// are merged into the remaining requirements. Conflicting effects or
    /// preconditions return <see cref="OperationStatus.Conflict"/>.
    /// </remarks>
    /// <param name="effectSets">Facts set by the action.</param>
    /// <param name="effectClearMask">Facts cleared by the action.</param>
    /// <param name="preconditions">Action preconditions to merge.</param>
    /// <param name="destination">Receives the regressed requirements.</param>
    /// <returns>Success or conflict/validation failure.</returns>
    public OperationStatus RegressThrough(
        SymbolicWorldState effectSets,
        ulong effectClearMask,
        SymbolicWorldState preconditions,
        SymbolicWorldState destination)
    {
        ArgumentNullException.ThrowIfNull(effectSets);
        ArgumentNullException.ThrowIfNull(preconditions);
        ArgumentNullException.ThrowIfNull(destination);

        if (ReferenceEquals(destination, this) ||
            ReferenceEquals(destination, effectSets) ||
            ReferenceEquals(destination, preconditions))
        {
            return OperationStatus.InvalidArgument;
        }

        if (DetectEffectConflict(effectSets, effectClearMask))
        {
            return OperationStatus.Conflict;
        }

        CopyTo(destination);
        RemoveSatisfiedByEffects(destination, effectSets, effectClearMask);
        return MergePreconditions(destination, preconditions);
    }

    /// <summary>
    /// Counts requirements in <paramref name="requirements"/> that this state does not satisfy.
    /// </summary>
    /// <param name="requirements">Required facts.</param>
    /// <returns>Unsatisfied fact count.</returns>
    public int CountUnsatisfied(SymbolicWorldState requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        int count = 0;
        ulong remaining = requirements.SpecifiedMask;
        while (remaining != 0UL)
        {
            int index = TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;

            if ((SpecifiedMask & bit) == 0UL || _values[index] != requirements._values[index])
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Computes a deterministic FNV-1a hash over the specified mask and values.
    /// </summary>
    /// <returns>64-bit hash.</returns>
    public ulong ComputeHash()
    {
        ulong hash = FnvOffset;
        hash = MixUInt64(hash, SpecifiedMask);

        ulong remaining = SpecifiedMask;
        while (remaining != 0UL)
        {
            int index = TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;
            hash = MixUInt64(hash, (ulong)(uint)_values[index]);
            hash = MixUInt64(hash, (ulong)(uint)index);
        }

        return hash;
    }

    /// <summary>
    /// Copies mask and values into <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">Destination state with equal or larger capacity.</param>
    /// <returns>Success or validation failure.</returns>
    public OperationStatus CopyTo(SymbolicWorldState destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (destination._capacity < _capacity)
        {
            return OperationStatus.InvalidArgument;
        }

        destination.SpecifiedMask = SpecifiedMask;
        for (int i = 0; i < _capacity; i++)
        {
            destination._values[i] = _values[i];
        }

        for (int i = _capacity; i < destination._capacity; i++)
        {
            destination._values[i] = 0;
        }

        return OperationStatus.Success;
    }

    /// <summary>
    /// Clears all specified facts.
    /// </summary>
    public void Reset()
    {
        SpecifiedMask = 0UL;
        for (int i = 0; i < _capacity; i++)
        {
            _values[i] = 0;
        }
    }

    /// <summary>
    /// Returns whether any required fact is established by the given effects.
    /// </summary>
    /// <param name="effectSets">Facts set by an action.</param>
    /// <param name="effectClearMask">Facts cleared by an action.</param>
    /// <returns><see langword="true"/> when at least one requirement is advanced.</returns>
    public bool IsAdvancedBy(SymbolicWorldState effectSets, ulong effectClearMask)
    {
        ArgumentNullException.ThrowIfNull(effectSets);

        ulong remaining = SpecifiedMask;
        while (remaining != 0UL)
        {
            int index = TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;

            if ((effectClearMask & bit) != 0UL)
            {
                continue;
            }

            if ((effectSets.SpecifiedMask & bit) != 0UL &&
                effectSets._values[index] == _values[index])
            {
                return true;
            }
        }

        return false;
    }

    private OperationStatus ApplyOneEffect(WorldStateEffect effect)
    {
        if (effect.Clears)
        {
            return Clear(effect.FactId);
        }

        return Set(effect.FactId, effect.Value);
    }

    private bool DetectEffectConflict(SymbolicWorldState effectSets, ulong effectClearMask)
    {
        if ((effectSets.SpecifiedMask & effectClearMask) != 0UL)
        {
            return true;
        }

        ulong remaining = SpecifiedMask & effectClearMask;
        if (remaining != 0UL)
        {
            return true;
        }

        return ConflictsWith(effectSets);
    }

    private static void RemoveSatisfiedByEffects(
        SymbolicWorldState destination,
        SymbolicWorldState effectSets,
        ulong effectClearMask)
    {
        ulong remaining = destination.SpecifiedMask;
        while (remaining != 0UL)
        {
            int index = TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;

            if ((effectClearMask & bit) != 0UL)
            {
                continue;
            }

            if ((effectSets.SpecifiedMask & bit) != 0UL &&
                effectSets._values[index] == destination._values[index])
            {
                destination.SpecifiedMask &= ~bit;
                destination._values[index] = 0;
            }
        }
    }

    private static OperationStatus MergePreconditions(
        SymbolicWorldState destination,
        SymbolicWorldState preconditions)
    {
        if (destination.ConflictsWith(preconditions))
        {
            return OperationStatus.Conflict;
        }

        ulong remaining = preconditions.SpecifiedMask;
        while (remaining != 0UL)
        {
            int index = TrailingZeroCount(remaining);
            ulong bit = 1UL << index;
            remaining &= ~bit;
            destination._values[index] = preconditions._values[index];
            destination.SpecifiedMask |= bit;
        }

        return OperationStatus.Success;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _capacity;
    }

    private static int TrailingZeroCount(ulong value)
    {
        return System.Numerics.BitOperations.TrailingZeroCount(value);
    }

    private static ulong MixUInt64(ulong hash, ulong data)
    {
        unchecked
        {
            hash ^= data;
            hash *= FnvPrime;
            return hash;
        }
    }
}
