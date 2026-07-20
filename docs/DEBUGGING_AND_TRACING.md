# Debugging and Tracing

Diagnostics help explain **why** an agent planned or failed without compromising
the frozen allocation path during normal ticks.

Related: [ARCHITECTURE.md](ARCHITECTURE.md),
[GOAP_PLANNER.md](GOAP_PLANNER.md),
[FAILURE_MODES.md](FAILURE_MODES.md),
[TESTING.md](TESTING.md).

## Ring buffer

- Cap: `MaximumDiagnosticRecords` (4096).
- Records are fixed-size structs: tick, agent, `DiagnosticSubsystem`, code,
  optional ids (goal, action, point).
- On overflow, overwrite oldest (ring) and bump a dropped counter.

## Subsystems (`DiagnosticSubsystem`)

Perception, Memory, TargetSelection, WeaponSelection, GoalArbitration,
CandidateGeneration, Planning, Execution, Navigation, Cover, Reservation,
Squad, Communication, Contract.

## Recommended traces

| Question | What to inspect |
|----------|-----------------|
| Why this goal? | GoalArbitration scores |
| Why this plan? | Planning expansions, final action ids, cost |
| Why replan? | Execution failure reason |
| Why idle? | No relevant goal / NoPlan |
| Why no cover? | Cover scores + reservation conflicts |
| Why squad stuck? | Order state + OrderFailed intents |

## Host dump

`TacticalGoap.Diagnostics` formats ring records **on demand** (allocations
allowed). Never format strings inside `[FrozenRuntimePath]` tick code.

## Contracts

Contract helpers assert lifecycle and capacity invariants in test/debug
configurations; violations map to `OperationStatus.ContractViolation`.

## Sample / audit

- Sample scenario logs digest lines for CI golden files.
- `TacticalGoap.Audit` flags Power-of-Ten issues offline.

## Implementation status

Enum catalog: **Implemented**. Ring buffer + formatters: **Pending**.
