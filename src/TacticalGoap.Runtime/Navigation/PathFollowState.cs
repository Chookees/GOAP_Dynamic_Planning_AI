using System;
using System.Diagnostics.CodeAnalysis;
using TacticalGoap.Abstractions.Identifiers;
using TacticalGoap.Abstractions.Results;

namespace TacticalGoap.Runtime.Navigation;

/// <summary>
/// Traversal link kinds that a path query may allow.
/// </summary>
[Flags]
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Flags suffix is conventional for [Flags] enumerations.")]
public enum NavigationQueryFlags : byte
{
    /// <summary>
    /// No special traversal links allowed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Door interaction links may be used.
    /// </summary>
    AllowDoorLinks = 1 << 0,

    /// <summary>
    /// Window traversal links may be used.
    /// </summary>
    AllowWindowLinks = 1 << 1,

    /// <summary>
    /// Vault traversal links may be used.
    /// </summary>
    AllowVaultLinks = 1 << 2,

    /// <summary>
    /// All supported traversal link kinds may be used.
    /// </summary>
    AllowAllTraversalLinks = AllowDoorLinks | AllowWindowLinks | AllowVaultLinks,
}

/// <summary>
/// Path-following lifecycle status for an agent.
/// </summary>
public enum PathFollowStatus : byte
{
    /// <summary>
    /// No active path.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// A path was accepted and is being followed.
    /// </summary>
    Following = 1,

    /// <summary>
    /// The agent reached the final path node.
    /// </summary>
    Arrived = 2,

    /// <summary>
    /// Pathfinding reported no walkable path.
    /// </summary>
    NoPath = 3,

    /// <summary>
    /// Pathfinding exceeded buffer or hard capacity.
    /// </summary>
    CapacityExceeded = 4,

    /// <summary>
    /// The active path was invalidated.
    /// </summary>
    Invalidated = 5,

    /// <summary>
    /// Follow failed for a host or validation reason.
    /// </summary>
    Failed = 6,
}

/// <summary>
/// Mutable path-follow state tracked by <see cref="NavigationOrchestrator"/>.
/// </summary>
public struct PathFollowState
{
    /// <summary>
    /// Gets or sets the follow lifecycle status.
    /// </summary>
    public PathFollowStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the agent associated with this follow state.
    /// </summary>
    public AgentId Agent { get; set; }

    /// <summary>
    /// Gets or sets the current path cursor index.
    /// </summary>
    public int Cursor { get; set; }

    /// <summary>
    /// Gets or sets the accepted path length.
    /// </summary>
    public int PathLength { get; set; }

    /// <summary>
    /// Gets or sets the query flags used for the active path.
    /// </summary>
    public NavigationQueryFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets the last host query status.
    /// </summary>
    public NavigationQueryStatus LastQueryStatus { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether door links were required by the path.
    /// </summary>
    public bool RequiresDoorLink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether window links were required by the path.
    /// </summary>
    public bool RequiresWindowLink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vault links were required by the path.
    /// </summary>
    public bool RequiresVaultLink { get; set; }

    /// <summary>
    /// Resets the state to idle.
    /// </summary>
    public void Reset()
    {
        Status = PathFollowStatus.Idle;
        Agent = AgentId.Invalid;
        Cursor = 0;
        PathLength = 0;
        Flags = NavigationQueryFlags.None;
        LastQueryStatus = NavigationQueryStatus.Success;
        RequiresDoorLink = false;
        RequiresWindowLink = false;
        RequiresVaultLink = false;
    }
}
