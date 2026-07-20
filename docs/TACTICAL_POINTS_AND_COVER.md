# Tactical Points and Cover

Tactical points are discrete, authored or generated positions agents can reserve
and occupy. Cover evaluation scores a bounded candidate set and feeds facts such
as `InCover` and `CoverValid`. Combined with GOAP regression, this produces
**emergent side-attack and flanking** without a dedicated flank script.

Related: [GOAP_PLANNER.md](GOAP_PLANNER.md),
[NAVIGATION_INTEGRATION.md](NAVIGATION_INTEGRATION.md),
[SQUAD_COORDINATION.md](SQUAD_COORDINATION.md),
[WORLD_STATE_MODEL.md](WORLD_STATE_MODEL.md).

## Tactical point model

- Id: `TacticalPointId`
- Cap: `MaximumTacticalPoints` = 256 per scenario
- Categories (`TacticalPointCategory`):

| Category | Use |
|----------|-----|
| `Cover` | Defensive posture |
| `Ambush` | Concealed attack |
| `Search` | Sector inspection stance |
| `Observation` | Overwatch |
| `Suppression` | Firing position for suppress |
| `Formation` | Squad slot |
| `DoorInteraction` | Door use pose |
| `WindowTraversal` / `VaultTraversal` | Traversal anchors |
| `Fallback` | Retreat |

Points carry integer metadata: cover quality, fire arcs, occupancy capacity,
linked navigation node.

## Cover evaluation

1. Gather up to `MaximumCoverCandidates` (32) nearby points (spatial query from
   host or precomputed adjacency).
2. Score with deterministic integer weights: protection vs. threat direction,
   distance, LOS to focus, occupancy, reservation conflicts.
3. Optionally prefer **side angles** (dot product of approach vs. threat facing
   quantized into bands) to favor flanking geometry.
4. Best point becomes a move / take-cover action candidate parameter.

Reservations use a global table capped at `MaximumReservations` (128). Conflicts
return `OperationStatus.Conflict`. Lost reservations map to
`ActionFailureReason.ReservationLost` / memory `CoverReservationLost`.

## Emergent side-attack / flanking demo

### Setup (sample scenario intent)

- Threat faces a primary cover line (front cover A, B).
- Side covers C, D offer LOS with higher “flank angle” score.
- Agent goal: survive under fire then attack (`InCover` + `TargetVisible` +
  attack desire).

### What happens

1. Under fire → arbitrator selects TakeCover / AttackFromCover goal.
2. Cover scorer ranks C/D above A/B due to side-angle weight.
3. Planner regresses: need `AtTacticalPoint` & `InCover` & `TargetVisible` →
   `MoveToTacticalPoint(C)` then `TakeCover` then `Attack`.
4. Path follows nav mesh to C—**appearing as a flank**—though no action is named
   “Flank”.

### Why this matches public GOAP pedagogy

Public F.E.A.R.-era discussions emphasize that interesting tactics emerge from
**goal + action vocabulary + world representation**, not from scripting each
maneuver. TacticalGoap’s demo documents that property for regression tests
(`REQ-COVER-001`, `REQ-SAMPLE-001`).

### Demo validation

Simulation tests should assert: under the scripted layout, the chosen tactical
point id is a side point (C or D), and the reconstructed plan includes move-to
that point before attack. Exact scenario assets land with the Sample project.

## Invalidation

Host or perception may mark cover compromised (`CoverInvalid` memory, clear
`CoverValid`). Running cover actions fail with `CoverInvalidated` and replan.

## Implementation status

Enums and limits: **Implemented**. Scoring, reservations, flanking demo:
**Pending / PartiallyImplemented** (`REQ-COVER-001`).
