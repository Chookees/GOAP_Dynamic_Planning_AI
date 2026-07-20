# Goal Arbitration

Each agent may register up to `AiHardLimits.MaximumGoalsPerAgent` (32) goals.
Every tick, after world-state quantization, the **goal arbitrator** selects at
most one active goal that drives planning and execution.

Related: [GOAP_PLANNER.md](GOAP_PLANNER.md),
[WORLD_STATE_MODEL.md](WORLD_STATE_MODEL.md),
[SQUAD_COORDINATION.md](SQUAD_COORDINATION.md),
[ACTION_EXECUTION.md](ACTION_EXECUTION.md).

## Goal definition

A goal registration (configuration-time) includes:

| Field | Purpose |
|-------|---------|
| `GoalId` | Stable identifier |
| Desire mask | `WorldFactId` bits that constitute success |
| Relevance predicate | Which facts must hold to be considered |
| Priority / utility | Integer score; higher wins |
| Interrupt policy | Whether a higher goal may cancel execution |
| Replan policy | When to rebuild plans |

Goals do not embed scripts; they declare **desired world conditions**.

## Arbitration algorithm

1. Collect goals whose relevance predicates pass under the current mask.
2. Apply squad bias: active `SquadOrderAvailable` may boost or inject a
   squad-serving goal.
3. Sort by priority, then by stable `GoalId` for determinism.
4. Select the top goal.
5. If the selected goal differs from the previous tick’s goal and interrupt is
   allowed, cancel the current action (`ActionStatus.Cancelled`) and discard
   the planner workspace.
6. If the same goal remains and a valid plan exists, continue execution.
7. Otherwise start or continue incremental planning toward the desire mask.

## Priority bands (recommended)

| Band | Examples |
|------|----------|
| Critical | Immediate danger, grenade flee |
| Combat | Attack, suppress, take cover under fire |
| Squad | Satisfy `SquadOrder*` |
| Tactical | Search, regroup, advance |
| Idle | `ReadinessMaintained`, patrol |

Exact numeric priorities are scenario configuration.

## Plan reuse

To avoid thrashing:

- Do not replan every tick if the plan’s next action preconditions still hold.
- Replan when: goal changes, volatile precondition fails, reservation lost,
  `PathValid` clears, or replanning attempts remain under
  `MaximumReplanningAttempts` (8).

## Interaction with squads

Squad behaviors set orders; agents treat orders as facts and elevated goals.
Individual planners still produce concrete action sequences. See
[SQUAD_COORDINATION.md](SQUAD_COORDINATION.md).

## Failure when no goal is relevant

If no goal is relevant, the agent holds (no-op execution). Diagnostics record
`DiagnosticSubsystem.GoalArbitration` with a no-goal event.

## Implementation status

`GoalId` and limits: **Implemented**. Arbitrator runtime:
**Pending** (`REQ-GOAP-001`).
