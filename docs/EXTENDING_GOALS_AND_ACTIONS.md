# Extending Goals and Actions

How to add new goals and actions without breaking planner invariants or capacity
rules.

Related: [GOAL_ARBITRATION.md](GOAL_ARBITRATION.md),
[ACTION_EXECUTION.md](ACTION_EXECUTION.md),
[WORLD_STATE_MODEL.md](WORLD_STATE_MODEL.md),
[GOAP_PLANNER.md](GOAP_PLANNER.md),
[CONFIGURATION.md](CONFIGURATION.md).

## Adding a world fact (rare)

1. Allocate the next `WorldFactId` value (< 64).
2. Document ownership in [WORLD_STATE_MODEL.md](WORLD_STATE_MODEL.md).
3. Add quantizer or action effect writers.
4. Update tests and fidelity notes if public behavior changes.

Prefer reusing existing facts when possible.

## Adding a goal

1. Choose desire mask from existing facts.
2. Define relevance predicate and priority band.
3. Register in configuration under an agent’s goal set
   (respect `MaximumGoalsPerAgent`).
4. Ensure at least one action chain can achieve the desire (planner regression
   smoke test).
5. Decide interrupt policy vs. squad orders.

## Adding an action definition

1. Specify precondition masks (true/false), effect masks, base cost
   (≤ `MaximumActionCost`).
2. Implement executor handlers for Starting/Running (host calls).
3. List volatile preconditions checked each tick.
4. Map host failures to `ActionFailureReason`.
5. Register definition (≤ `MaximumActionDefinitions`).

## Adding candidate generators

Generators bind parameters (target, point, door) into candidates. Keep
generation ≤ `MaximumActionCandidates` and deterministic in ordering.

## Checklist

- [ ] No allocations on frozen executor path
- [ ] Costs use checked arithmetic
- [ ] Effects are consistent with quantizers (no contradictory owners)
- [ ] Unit tests for applicability and a planner worked example
- [ ] Diagnostics subsystem tags (`Planning` / `Execution`)

## Implementation status

Extension process: **Documented**. Registration APIs: **Pending**.
