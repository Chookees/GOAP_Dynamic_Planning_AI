# Squad Coordination

Squads layer **shared behaviors and orders** on top of individual GOAP agents.
At most one `SquadBehaviorType` is active per squad. Orders are discrete tasks
assigned to members; agents satisfy them through normal planning.

Related: [GOAL_ARBITRATION.md](GOAL_ARBITRATION.md),
[COMMUNICATION_SYSTEM.md](COMMUNICATION_SYSTEM.md),
[TACTICAL_POINTS_AND_COVER.md](TACTICAL_POINTS_AND_COVER.md).

## Capacities

| Limit | Value |
|-------|------:|
| `MaximumSquads` | 16 |
| `MaximumAgentsPerSquad` | 8 |
| `MaximumSquadOrders` | 64 |
| `MaximumSearchSectors` | 32 |

## Behaviors (`SquadBehaviorType`)

| Behavior | Intent |
|----------|--------|
| `None` | No coordination |
| `GetToCover` | Move members to cover; optional suppress |
| `AdvanceCover` | Bound forward using cover + suppression |
| `OrderlyAdvance` | Formation slots |
| `Search` | Assign sectors |
| `Regroup` | Rally separated agents |
| `HoldPosition` | Hold |

Behavior selection is configuration- and threat-driven, evaluated on a squad tick
phase before agent arbitration.

## Orders (`SquadOrderType`)

| Order | Typical desire facts |
|-------|----------------------|
| `MoveToCover` | `AtTacticalPoint`, `InCover` |
| `ProvideSuppression` | `Suppressing` |
| `Advance` | `AtMovementDestination` |
| `Hold` | Hold posture / readiness |
| `SearchSector` | `SearchLocationInspected` |
| `FollowFormation` | Formation point occupancy |
| `Regroup` | Rally arrival |
| `Observe` | Observation point |

Orders carry `OrderId`, assignee `AgentId`, optional point/sector ids, and an
expiry tick. Facts: `SquadOrderAvailable`, `SquadOrderSatisfied`.

## Coordination loop

1. Update squad threat picture from member memory / communications.
2. Select or refresh behavior.
3. Allocate orders without exceeding order table capacity.
4. Members raise squad-biased goals; plan individually.
5. On member success/fail, update order state and emit communication intents
   (`OrderAcknowledged`, `OrderFailed`).

## Conflict resolution

- Cover reservations prevent two agents claiming one capacity slot.
- If an order cannot be planned (`NoPlan`), mark order failed and reassign.
- Individual critical goals (grenade danger) may interrupt squad orders when
  interrupt policy allows.

## Implementation status

Enums and limits: **Implemented**. Coordinator: **Pending**
(`REQ-SQUAD-001`).
