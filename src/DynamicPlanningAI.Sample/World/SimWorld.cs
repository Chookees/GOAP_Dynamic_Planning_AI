using System;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Sample.Hosting;

namespace DynamicPlanningAI.Sample.World;

/// <summary>
/// Mutable sample world holding agents, tactical points, dangers, and smart objects.
/// </summary>
public sealed class SimWorld
{
    private readonly SimAgentState[] _agents;
    private readonly Int2[] _tacticalPositions;
    private readonly TacticalPointCategory[] _tacticalCategories;
    private readonly Int2[] _dangers;
    private readonly Int2[] _smartObjectPositions;
    private readonly int[] _smartObjectStates;
    private readonly bool[] _smartObjectAvailable;
    private int _agentCount;
    private int _tacticalCount;
    private int _dangerCount;
    private int _smartObjectCount;
    private int _fireCount;
    private int _commCount;

    /// <summary>
    /// Initializes a world bound to a grid map.
    /// </summary>
    public SimWorld(GridMap map, int visionRange)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        VisionRange = visionRange;
        _agents = new SimAgentState[AiHardLimits.MaximumAgents];
        _tacticalPositions = new Int2[AiHardLimits.MaximumTacticalPoints];
        _tacticalCategories = new TacticalPointCategory[AiHardLimits.MaximumTacticalPoints];
        _dangers = new Int2[AiHardLimits.MaximumDangerEvents];
        _smartObjectPositions = new Int2[32];
        _smartObjectStates = new int[32];
        _smartObjectAvailable = new bool[32];
        _agentCount = 0;
        _tacticalCount = 0;
        _dangerCount = 0;
        _smartObjectCount = 0;
        _fireCount = 0;
        _commCount = 0;
    }

    /// <summary>Gets the grid map.</summary>
    public GridMap Map { get; }

    /// <summary>Gets the vision range in cells.</summary>
    public int VisionRange { get; }

    /// <summary>Gets the agent count.</summary>
    public int AgentCount => _agentCount;

    /// <summary>Gets the tactical point count.</summary>
    public int TacticalPointCount => _tacticalCount;

    /// <summary>Gets the danger count.</summary>
    public int DangerCount => _dangerCount;

    /// <summary>Gets cumulative fire events.</summary>
    public int FireCount => _fireCount;

    /// <summary>Gets cumulative communication events.</summary>
    public int CommunicationCount => _commCount;

    /// <summary>
    /// Adds an agent.
    /// </summary>
    public SimAgentState AddAgent(AgentId id, EntityId entityId, Int2 position, bool isHostile)
    {
        if (_agentCount >= _agents.Length)
        {
            throw new InvalidOperationException("Agent capacity exceeded.");
        }

        SimAgentState agent = new(id, entityId, position, isHostile);
        _agents[_agentCount] = agent;
        _agentCount = checked(_agentCount + 1);
        return agent;
    }

    /// <summary>Gets an agent by slot.</summary>
    public SimAgentState GetAgentAt(int index) => _agents[index];

    /// <summary>Tries to get an agent by id.</summary>
    public bool TryGetAgent(AgentId id, out SimAgentState? agent)
    {
        for (int i = 0; i < _agentCount; i++)
        {
            if (_agents[i].Id == id)
            {
                agent = _agents[i];
                return true;
            }
        }

        agent = null;
        return false;
    }

    /// <summary>Requires an agent by id.</summary>
    public SimAgentState RequireAgent(AgentId id)
    {
        if (!TryGetAgent(id, out SimAgentState? agent) || agent is null)
        {
            throw new InvalidOperationException("Agent not found.");
        }

        return agent;
    }

    /// <summary>Tries to get an agent by entity id.</summary>
    public bool TryGetByEntity(EntityId entity, out SimAgentState? agent)
    {
        for (int i = 0; i < _agentCount; i++)
        {
            if (_agents[i].EntityId == entity)
            {
                agent = _agents[i];
                return true;
            }
        }

        agent = null;
        return false;
    }

    /// <summary>Adds a tactical point.</summary>
    public TacticalPointId AddTacticalPoint(Int2 position, TacticalPointCategory category)
    {
        if (_tacticalCount >= _tacticalPositions.Length)
        {
            throw new InvalidOperationException("Tactical point capacity exceeded.");
        }

        int index = _tacticalCount;
        _tacticalPositions[index] = position;
        _tacticalCategories[index] = category;
        _tacticalCount = checked(_tacticalCount + 1);
        if (category == TacticalPointCategory.Cover)
        {
            Map.SetKind(position, CellKind.Cover);
        }

        return TacticalPointId.FromInt32(index);
    }

    /// <summary>Gets a tactical point position.</summary>
    public Int2 GetTacticalPosition(int index) => _tacticalPositions[index];

    /// <summary>Gets a tactical point category.</summary>
    public TacticalPointCategory GetTacticalCategory(int index) => _tacticalCategories[index];

    /// <summary>Adds a danger cell.</summary>
    public void AddDanger(Int2 position)
    {
        if (_dangerCount >= _dangers.Length)
        {
            return;
        }

        _dangers[_dangerCount] = position;
        _dangerCount = checked(_dangerCount + 1);
    }

    /// <summary>Clears all danger cells.</summary>
    public void ClearDanger() => _dangerCount = 0;

    /// <summary>Gets a danger cell.</summary>
    public Int2 GetDangerAt(int index) => _dangers[index];

    /// <summary>Registers a smart object (door/window).</summary>
    public SmartObjectId AddSmartObject(Int2 position, int stateCode, bool available)
    {
        if (_smartObjectCount >= _smartObjectPositions.Length)
        {
            throw new InvalidOperationException("Smart object capacity exceeded.");
        }

        int index = _smartObjectCount;
        _smartObjectPositions[index] = position;
        _smartObjectStates[index] = stateCode;
        _smartObjectAvailable[index] = available;
        _smartObjectCount = checked(_smartObjectCount + 1);
        return SmartObjectId.FromInt32(index);
    }

    /// <summary>Records a fire event.</summary>
    public void RecordFire(AgentId agent, EntityId target) => _fireCount = checked(_fireCount + 1);

    /// <summary>Records a communication event.</summary>
    public void RecordCommunication(AgentId speaker, int intent, EntityId related) =>
        _commCount = checked(_commCount + 1);

    /// <summary>Attempts smart-object interaction (doors).</summary>
    public OperationStatus TryInteractSmartObject(AgentId agent, SmartObjectId smartObject)
    {
        if (!smartObject.IsValid || smartObject.Value >= _smartObjectCount)
        {
            return OperationStatus.NotFound;
        }

        Int2 pos = _smartObjectPositions[smartObject.Value];
        if (Map.GetKind(pos) == CellKind.Door)
        {
            if (Map.IsDoorBlocked(pos) && !Map.IsDoorOpen(pos))
            {
                return OperationStatus.Failed;
            }

            Map.OpenDoor(pos);
            _smartObjectStates[smartObject.Value] = 1;
            return OperationStatus.Success;
        }

        return OperationStatus.Success;
    }

    /// <summary>Attempts to breach a door smart object.</summary>
    public OperationStatus TryBreachSmartObject(SmartObjectId smartObject)
    {
        if (!smartObject.IsValid || smartObject.Value >= _smartObjectCount)
        {
            return OperationStatus.NotFound;
        }

        Int2 pos = _smartObjectPositions[smartObject.Value];
        if (!Map.TryBreachDoor(pos))
        {
            return OperationStatus.Failed;
        }

        _smartObjectStates[smartObject.Value] = 2;
        return OperationStatus.Success;
    }

    /// <summary>Reads smart object position.</summary>
    public OperationStatus TryGetSmartObjectPosition(SmartObjectId smartObject, out Int2 position)
    {
        if (!smartObject.IsValid || smartObject.Value >= _smartObjectCount)
        {
            position = Int2.Zero;
            return OperationStatus.NotFound;
        }

        position = _smartObjectPositions[smartObject.Value];
        return OperationStatus.Success;
    }

    /// <summary>Returns smart object availability.</summary>
    public bool IsSmartObjectAvailable(SmartObjectId smartObject) =>
        smartObject.IsValid &&
        smartObject.Value < _smartObjectCount &&
        _smartObjectAvailable[smartObject.Value];

    /// <summary>Reads smart object state.</summary>
    public OperationStatus TryGetSmartObjectState(SmartObjectId smartObject, out int stateCode)
    {
        if (!smartObject.IsValid || smartObject.Value >= _smartObjectCount)
        {
            stateCode = 0;
            return OperationStatus.NotFound;
        }

        stateCode = _smartObjectStates[smartObject.Value];
        return OperationStatus.Success;
    }
}
