# Navigation Integration

TacticalGoap does **not** embed a navigation mesh implementation. The runtime
orchestrates movement actions by calling a **host navigation service** and
mirroring results into world facts and failure reasons.

Related: [ACTION_EXECUTION.md](ACTION_EXECUTION.md),
[TACTICAL_POINTS_AND_COVER.md](TACTICAL_POINTS_AND_COVER.md),
[ENGINE_INTEGRATION_GUIDE.md](ENGINE_INTEGRATION_GUIDE.md),
[DETERMINISM.md](DETERMINISM.md).

## Identifiers

| Type | Role |
|------|------|
| `NavigationNodeId` | Discrete graph / poly id |
| `NavigationAreaId` | Area / room / zone |
| Path buffer | Up to `MaximumNavigationPathNodes` (128) nodes |

## Host service contract (conceptual)

Hosts implement operations such as:

- `TryFindPath(agent, destination, Span<NavigationNodeId> outPath) → OperationStatus`
- `TryFollowPath(agent, tick) → OperationStatus` (InProgress / Success / Failed)
- `InvalidatePath(agent)` when world changes
- Optional: link queries for doors, windows, vaults

Determinism requirements:

- Same nav mesh + same queries + same tick ⇒ same path
- No asynchronous callbacks that complete in wall-clock order; completion must be
  applied on a specific `AiTick.Sequence`

## World facts

| Fact | Set when |
|------|----------|
| `MovementDestinationSet` | Destination accepted |
| `PathValid` | Path exists and not invalidated |
| `AtMovementDestination` | Arrival tolerance met |
| `TraversalAvailable` | Special link usable |
| `AtWindowTraversal` / `WindowTraversed` | Window flow |

## Action failures

| Reason | When |
|--------|------|
| `NoPath` | FindPath failed |
| `PathInvalidated` | Mesh change / dynamic blocker |
| `DestinationOccupied` | Capacity / reservation |
| `TraversalUnavailable` | Link disabled |
| `DoorBlocked` / `DoorDestroyed` | Door interactions |

Memory types `NavigationFailed` and `TraversalFailed` capture evidence for
later goals.

## Integration with tactical points

Move-to-cover actions resolve a `TacticalPointId` to a navigation destination
via authoring links. The planner sees symbolic facts; the executor talks to the
nav host.

## Engine examples (non-exhaustive)

| Engine | Adapter notes |
|--------|---------------|
| Custom / sample | Grid or waypoint graph in console sim |
| Unity | `NavMesh.CalculatePath` wrapped; pump results on FixedUpdate tick |
| Unreal | `AIController` path following; marshal on AI tick |
| Godot | `NavigationAgent3D`; sync on physics frame |

See [ENGINE_INTEGRATION_GUIDE.md](ENGINE_INTEGRATION_GUIDE.md).

## Implementation status

Identifiers and limits: **Implemented**. Host interfaces and orchestration:
**Pending** (`REQ-NAV-001`).
