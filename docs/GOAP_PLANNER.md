# GOAP Planner

TacticalGoap plans with **backward regression A\***: search starts from the goal’s
desired world facts and regresses through action effects until the current world
state satisfies remaining needs—or budgets fail with an explicit
`PlannerStatus`.

Related: [WORLD_STATE_MODEL.md](WORLD_STATE_MODEL.md),
[GOAL_ARBITRATION.md](GOAL_ARBITRATION.md),
[ACTION_EXECUTION.md](ACTION_EXECUTION.md),
[DESIGN_DECISIONS.md](DESIGN_DECISIONS.md) (ADR-001, ADR-002),
[FAILURE_MODES.md](FAILURE_MODES.md).

## Why backward regression

Forward search expands every applicable action from the current state; the
branching factor grows with the action library. Backward regression expands only
actions whose **effects** intersect unsatisfied goal conditions, which matches
tactical GOAP libraries where many actions are situational and goals are sparse
bit patterns.

## Inputs

| Input | Description |
|-------|-------------|
| Current `WorldState` | Bit-mask of `WorldFactId` facts |
| Goal desire mask | Facts that must be true for success |
| Action candidates | Instantiated actions with preconditions, effects, cost |
| Budgets | Nodes, open-set, expansions per step, plan length |

Candidates are generated before planning (or refreshed when world focus changes).
Hard caps: `MaximumActionCandidates` (256), `MaximumPlannerNodes` (512),
`MaximumPlanLength` (16), `MaximumPlannerExpansionsPerStep` (64),
`MaximumPlannerStepsPerTick` (8).

## Search sketch

1. **Start node** — unsatisfied = `goalDesire & ~currentState`.
2. **Open set** — custom binary heap ordered by `f = g + h` with deterministic
   tie-break (node id, then action candidate id). See ADR-002.
3. **Expand** — for each unsatisfied fact bit, consider candidates whose effects
   set that bit; regress preconditions into the child unsatisfied mask; add cost.
4. **Goal test** — unsatisfied mask empty ⇒ reconstruct plan by parent links
   (reverse to execution order).
5. **Incremental step** — stop when expansions for this call hit the step budget;
   return `InProgress` so the agent can continue next tick.

Heuristic `h` is admissible for unit/positive costs: count of remaining
unsatisfied facts (or a weighted variant configured at freeze time). Costs are
checked integers; overflow yields `PlannerStatus.CostOverflow`.

## PlannerResult

`PlannerResult` carries:

- `Status` — see table below
- `ExpansionsPerformed`, `NodesAllocated`, `PlanLength`
- `GoalId`, `TotalCost`

Callers must check `Status` / `IsSuccess` before consuming plan data.

### PlannerStatus meanings

| Status | Meaning | Caller action |
|--------|---------|---------------|
| `InProgress` | Step budget hit | Continue next tick |
| `Succeeded` | Plan reconstructed | Install plan into executor |
| `NoPlan` | Search exhausted | Fail goal / try fallback goal |
| `ExpansionBudgetExceeded` | Step stop (may alias InProgress policy) | Continue or abandon per config |
| `NodeCapacityExceeded` | Node table full | Soft-fail; reset workspace |
| `OpenSetCapacityExceeded` | Heap full | Soft-fail; reset workspace |
| `ClosedSetCapacityExceeded` | Duplicate table full | Soft-fail; reset workspace |
| `PlanLengthExceeded` | Path too long | Reject; try cheaper goal |
| `InvalidGoal` / `InvalidWorldState` / `InvalidActionCandidate` | Bad input | Fix registration |
| `Cancelled` | Higher priority interrupted | Discard workspace |
| `CostOverflow` | Checked math failed | Treat as no plan |

Capacity failures leave the workspace reusable after a deterministic reset.

## Worked example: BasicAttack

**World facts of interest:**

- `TargetSelected`, `TargetKnown`, `TargetVisible`, `TargetAlive`
- `WeaponSelected`, `WeaponLoaded`, `HasAmmunition`
- `AtTacticalPoint`, `InCover` (optional for safer attack)

**Goal:** Combat success staged through attack preconditions. A minimal teaching
desire toward `Attack` includes establishing `WeaponLoaded` and `TargetVisible`
while `TargetSelected`, `TargetAlive`, `WeaponSelected`, and `HasAmmunition`
already hold.

**Suppose current state:**

```
TargetSelected | TargetKnown | TargetAlive | WeaponSelected | HasAmmunition
```

**Missing for Attack:** `WeaponLoaded`, `TargetVisible`.

**Regression:**

1. Need `WeaponLoaded` ← effect of `Reload` (pre: `WeaponSelected`, `HasAmmunition`).
2. Need `TargetVisible` ← effect of `MoveToPeek` or `AcquireLOS` (pre: path /
   tactical point facts).
3. When both are satisfied relative to current state, `Attack` becomes applicable.

**Example reconstructed plan (execution order):**

1. `MoveToPeek` (establishes LOS / `TargetVisible`)
2. `Reload` (establishes `WeaponLoaded`)
3. `Attack`

Costs might be 5 + 2 + 1 = 8. If cover is invalidated mid-execution,
`ActionFailureReason.CoverInvalidated` triggers replan (see
[ACTION_EXECUTION.md](ACTION_EXECUTION.md)).

### Emergent flanking note

When cover scoring prefers points with side angles and the planner regresses
`InCover` + `TargetVisible`, agents often produce **side-attack / flanking**
paths without a dedicated “Flank” script. See
[TACTICAL_POINTS_AND_COVER.md](TACTICAL_POINTS_AND_COVER.md).

## Incremental planning and multitasking

Per tick, an agent may call the planner at most
`MaximumPlannerStepsPerTick` times. Long searches spread across frames,
preserving frame budget ([PERFORMANCE.md](PERFORMANCE.md)).

## Implementation status

Abstractions result types and limits: **Implemented**. Planner core and candidate
generation: **PartiallyImplemented / Pending** per
[REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md) (`REQ-GOAP-001`).
