namespace TacticalGoap.Runtime.Planning;

/// <summary>
/// Storage fields for a single planner search node.
/// </summary>
public struct PlannerNode
{
    /// <summary>
    /// Gets or sets the parent node index, or -1 for the root.
    /// </summary>
    public int ParentIndex { get; set; }

    /// <summary>
    /// Gets or sets the action candidate index that produced this node, or -1 for the root.
    /// </summary>
    public int CandidateIndex { get; set; }

    /// <summary>
    /// Gets or sets the path cost from the goal requirements to this node.
    /// </summary>
    public int GCost { get; set; }

    /// <summary>
    /// Gets or sets the heuristic estimate remaining (always zero in Dijkstra mode).
    /// </summary>
    public int HCost { get; set; }

    /// <summary>
    /// Gets or sets the hash of the regressed requirement state.
    /// </summary>
    public ulong StateHash { get; set; }

    /// <summary>
    /// Gets or sets the index into the workspace world-state pool for this node's requirements.
    /// </summary>
    public int StateIndex { get; set; }

    /// <summary>
    /// Gets or sets the insertion ordinal used for deterministic open-set tie-breaking.
    /// </summary>
    public int InsertionOrder { get; set; }
}
