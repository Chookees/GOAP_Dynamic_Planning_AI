# Configuration

`DynamicPlanningAI.Configuration` loads and validates immutable scenario definitions,
then hands validated sizes and definitions to Runtime for freeze.

Related: [ARCHITECTURE.md](ARCHITECTURE.md),
[MEMORY_AND_ALLOCATIONS.md](MEMORY_AND_ALLOCATIONS.md),
[ENGINE_INTEGRATION_GUIDE.md](ENGINE_INTEGRATION_GUIDE.md),
[EXTENDING_GOALS_AND_ACTIONS.md](EXTENDING_GOALS_AND_ACTIONS.md).

## Principles

1. **Fail at load**, not mid-combat: invalid configs never reach `Running`.
2. Every capacity field is clamped to `AiHardLimits` and rejected if ≤ 0 when
   required.
3. Definitions are immutable after successful validation.
4. Prefer JSON (via `System.Text.Json`) for sample/scenarios; hosts may supply
   objects directly without serialization.

## Typical config sections

| Section | Contents |
|---------|----------|
| Runtime capacities | Agents, squads, planner budgets (≤ hard limits) |
| Agents | Ids, squad membership, goal set references |
| Goals | Desire masks, priorities, interrupt flags |
| Actions | Preconditions/effects masks, base costs |
| Weapons | Range bands, ammo rules |
| Tactical points | Category, links, capacities |
| Cover weights | Scoring integers including flank-angle weight |
| Squads | Behaviors, sector assignments |
| Quantizer thresholds | Distance/angle bands |
| Diagnostics | Ring buffer size, enabled subsystems |

## Validation checklist

- Unique ids within each namespace
- Goal desire bits ⊆ defined `WorldFactId` range
- Action effect/precondition bits valid
- Squad membership ≤ `MaximumAgentsPerSquad`
- Planner budgets: expansions, nodes, plan length coherent
- Tick delta max ≤ `MaximumTickDeltaMilliseconds`

Failures surface as structured validation errors (configuration layer may
allocate freely).

## Dependency note

Configuration → Runtime → Abstractions. Configuration must not be referenced by
Runtime. Sample and tools reference Configuration for loading.

## Implementation status

Project scaffolding: **PartiallyImplemented**. Loaders/schemas: **Pending**.
