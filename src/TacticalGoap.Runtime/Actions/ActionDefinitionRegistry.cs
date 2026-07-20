using System;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Fixed-capacity registry of immutable action definitions and executors.
/// </summary>
public sealed class ActionDefinitionRegistry
{
    private readonly ActionDefinition[] _definitions;
    private readonly IGoapActionExecutor?[] _executors;
    private readonly int _capacity;
    private int _count;

    /// <summary>
    /// Initializes a registry with hard-limit capacity.
    /// </summary>
    public ActionDefinitionRegistry()
        : this(AiHardLimits.MaximumActionDefinitions)
    {
    }

    /// <summary>
    /// Initializes a registry with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Maximum definitions.</param>
    public ActionDefinitionRegistry(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumActionDefinitions)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
        _definitions = new ActionDefinition[capacity];
        _executors = new IGoapActionExecutor?[capacity];
        _count = 0;
    }

    /// <summary>
    /// Gets the number of registered definitions.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Registers a definition and optional executor.
    /// </summary>
    /// <param name="definition">Immutable action definition.</param>
    /// <param name="executor">Optional runtime executor.</param>
    /// <returns>Success, conflict, or capacity failure.</returns>
    public OperationStatus Register(ActionDefinition definition, IGoapActionExecutor? executor)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (_count >= _capacity)
        {
            return OperationStatus.CapacityExceeded;
        }

        for (int i = 0; i < _count; i++)
        {
            if (_definitions[i].Id == definition.Id)
            {
                return OperationStatus.Conflict;
            }
        }

        if (executor is not null && executor.DefinitionId != definition.Id)
        {
            return OperationStatus.InvalidArgument;
        }

        _definitions[_count] = definition;
        _executors[_count] = executor;
        _count = checked(_count + 1);
        return OperationStatus.Success;
    }

    /// <summary>
    /// Attempts to resolve a definition by identifier.
    /// </summary>
    /// <param name="id">Definition identifier.</param>
    /// <param name="definition">Receives the definition.</param>
    /// <returns><see langword="true"/> when found.</returns>
    public bool TryGetDefinition(ActionId id, out ActionDefinition? definition)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_definitions[i].Id == id)
            {
                definition = _definitions[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    /// <summary>
    /// Attempts to resolve an executor by definition identifier.
    /// </summary>
    /// <param name="id">Definition identifier.</param>
    /// <param name="executor">Receives the executor.</param>
    /// <returns><see langword="true"/> when found.</returns>
    public bool TryGetExecutor(ActionId id, out IGoapActionExecutor? executor)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_definitions[i].Id == id)
            {
                executor = _executors[i];
                return executor is not null;
            }
        }

        executor = null;
        return false;
    }

    /// <summary>
    /// Copies registered definitions into a caller buffer.
    /// </summary>
    /// <param name="destination">Caller-owned buffer.</param>
    /// <param name="written">Receives written count.</param>
    /// <returns>Success or capacity failure.</returns>
    public OperationStatus CopyDefinitions(Span<ActionDefinition> destination, out int written)
    {
        if (destination.Length < _count)
        {
            written = 0;
            return OperationStatus.CapacityExceeded;
        }

        for (int i = 0; i < _count; i++)
        {
            destination[i] = _definitions[i];
        }

        written = _count;
        return OperationStatus.Success;
    }
}
