# Failure Modes

Expected AI failures are **explicit statuses**, not exceptions. This document
catalogs failure classes and recovery policies.

Related: [GOAP_PLANNER.md](GOAP_PLANNER.md),
[ACTION_EXECUTION.md](ACTION_EXECUTION.md),
[DEBUGGING_AND_TRACING.md](DEBUGGING_AND_TRACING.md),
[SQUAD_COORDINATION.md](SQUAD_COORDINATION.md).

## OperationStatus (cross-cutting)

| Status | Recovery |
|--------|----------|
| `Success` / `NoOp` / `InProgress` | Continue |
| `InvalidArgument` | Fix caller / config |
| `InvalidLifecycleState` | Wrong phase API use |
| `CapacityExceeded` | Soft-fail; diagnostics; do not resize |
| `NotFound` | Missing registration |
| `Conflict` | Reservation / ownership clash; pick alternate |
| `HostRejected` | Map to action failure |
| `TimedOut` | Abort action / order |
| `Cancelled` | Higher priority took over |
| `ContractViolation` | Bug; assert in tests |
| `ArithmeticOverflow` | Treat as planning/action failure |
| `Failed` | Last-resort explicit fail |

## Planner failures

See `PlannerStatus` table in [GOAP_PLANNER.md](GOAP_PLANNER.md). Capacity
failures reset the workspace deterministically and allow the agent to try a
fallback goal or wait.

## Action failures

See `ActionFailureReason` in [ACTION_EXECUTION.md](ACTION_EXECUTION.md). Policy:

1. Release reservations.
2. Write memory evidence when useful (`CoverInvalid`, `NavigationFailed`, …).
3. Replan if attempts < `MaximumReplanningAttempts`.
4. Otherwise fail the goal and re-arbitrate.

## Perception overflow

Dropping events is a failure mode of **information**, not of the process. Record
drops; agents may act on stale beliefs.

## Squad failures

Order expiry → `OrderExpired` / `SquadOrderFailed` memory → coordinator
reassigns or changes behavior.

## What must never happen

- Silent catch-and-ignore of planning errors
- Unbounded retry loops
- Throwing across host tick for expected combat contingencies
- Resizing frozen buffers

## Implementation status

Status enums: **Implemented**. Policy engines: **Pending**.
