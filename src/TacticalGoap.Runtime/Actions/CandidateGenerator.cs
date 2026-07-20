using System;
using TacticalGoap.Abstractions.Hosting;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Limits;
using TacticalGoap.Runtime.Planning;
using TacticalGoap.Runtime.WorldState;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Bounded inputs for action candidate generation.
/// </summary>
public readonly struct CandidateGenerationContext
{
    /// <summary>
    /// Initializes candidate-generation inputs.
    /// </summary>
    /// <param name="agentId">Agent requesting candidates.</param>
    /// <param name="worldState">Current symbolic world state.</param>
    /// <param name="focusEntity">Optional focus entity.</param>
    /// <param name="focusPoint">Optional focus tactical point.</param>
    /// <param name="focusNode">Optional focus navigation node.</param>
    /// <param name="focusWeapon">Optional focus weapon.</param>
    /// <param name="focusOrder">Optional focus order.</param>
    /// <param name="focusSector">Optional focus search sector.</param>
    /// <param name="tacticalPoints">Optional tactical-point provider.</param>
    /// <param name="smartObjects">Optional smart-object service.</param>
    public CandidateGenerationContext(
        AgentId agentId,
        SymbolicWorldState worldState,
        EntityId focusEntity,
        TacticalPointId focusPoint,
        NavigationNodeId focusNode,
        WeaponId focusWeapon,
        OrderId focusOrder,
        SearchSectorId focusSector,
        ITacticalPointProvider? tacticalPoints,
        ISmartObjectService? smartObjects)
    {
        ArgumentNullException.ThrowIfNull(worldState);
        AgentId = agentId;
        WorldState = worldState;
        FocusEntity = focusEntity;
        FocusPoint = focusPoint;
        FocusNode = focusNode;
        FocusWeapon = focusWeapon;
        FocusOrder = focusOrder;
        FocusSector = focusSector;
        TacticalPoints = tacticalPoints;
        SmartObjects = smartObjects;
    }

    /// <summary>Gets the agent identifier.</summary>
    public AgentId AgentId { get; }

    /// <summary>Gets the world state.</summary>
    public SymbolicWorldState WorldState { get; }

    /// <summary>Gets the focus entity.</summary>
    public EntityId FocusEntity { get; }

    /// <summary>Gets the focus tactical point.</summary>
    public TacticalPointId FocusPoint { get; }

    /// <summary>Gets the focus navigation node.</summary>
    public NavigationNodeId FocusNode { get; }

    /// <summary>Gets the focus weapon.</summary>
    public WeaponId FocusWeapon { get; }

    /// <summary>Gets the focus order.</summary>
    public OrderId FocusOrder { get; }

    /// <summary>Gets the focus search sector.</summary>
    public SearchSectorId FocusSector { get; }

    /// <summary>Gets the optional tactical-point provider.</summary>
    public ITacticalPointProvider? TacticalPoints { get; }

    /// <summary>Gets the optional smart-object service.</summary>
    public ISmartObjectService? SmartObjects { get; }
}

/// <summary>
/// Fixed-capacity writer for action candidates.
/// </summary>
public sealed class BoundedCandidateWriter
{
    private readonly ActionCandidate[] _buffer;
    private int _count;
    private int _nextCandidateValue;

    /// <summary>
    /// Initializes a writer with hard-limit capacity.
    /// </summary>
    public BoundedCandidateWriter()
        : this(AiHardLimits.MaximumActionCandidates)
    {
    }

    /// <summary>
    /// Initializes a writer with an explicit capacity.
    /// </summary>
    /// <param name="capacity">Maximum candidates.</param>
    public BoundedCandidateWriter(int capacity)
    {
        if (capacity < 1 || capacity > AiHardLimits.MaximumActionCandidates)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _buffer = new ActionCandidate[capacity];
        _count = 0;
        _nextCandidateValue = 0;
    }

    /// <summary>Gets the written candidate count.</summary>
    public int Count => _count;

    /// <summary>Gets the underlying buffer.</summary>
    public ActionCandidate[] Buffer => _buffer;

    /// <summary>Clears written candidates without releasing capacity.</summary>
    public void Reset()
    {
        _count = 0;
        _nextCandidateValue = 0;
    }

    /// <summary>
    /// Attempts to append a candidate.
    /// </summary>
    /// <param name="candidate">Candidate to append.</param>
    /// <returns><see langword="true"/> when accepted.</returns>
    public bool TryAdd(ActionCandidate candidate)
    {
        if (_count >= _buffer.Length)
        {
            return false;
        }

        if (!candidate.CandidateId.IsValid)
        {
            candidate.CandidateId = ActionCandidateId.FromInt32(_nextCandidateValue);
            _nextCandidateValue = checked(_nextCandidateValue + 1);
        }

        _buffer[_count] = candidate;
        _count = checked(_count + 1);
        return true;
    }

    /// <summary>
    /// Returns a span of written candidates.
    /// </summary>
    /// <returns>Written candidate span.</returns>
    public Span<ActionCandidate> AsSpan() => _buffer.AsSpan(0, _count);
}

/// <summary>
/// Generates grounded action candidates for planning.
/// </summary>
public interface IActionCandidateGenerator
{
    /// <summary>
    /// Gets the definition identifier this generator serves.
    /// </summary>
    public ActionId DefinitionId { get; }

    /// <summary>
    /// Appends zero or more candidates into the writer.
    /// </summary>
    /// <param name="context">Generation inputs.</param>
    /// <param name="definition">Action definition template.</param>
    /// <param name="writer">Bounded candidate writer.</param>
    /// <returns>Number of candidates appended.</returns>
    public int Generate(
        in CandidateGenerationContext context,
        ActionDefinition definition,
        BoundedCandidateWriter writer);
}
