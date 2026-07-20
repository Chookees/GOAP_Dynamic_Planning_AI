# Performance

Performance targets favor **bounded worst-case work per tick** over average-case
micro-optimizations.

Related: [GOAP_PLANNER.md](GOAP_PLANNER.md),
[MEMORY_AND_ALLOCATIONS.md](MEMORY_AND_ALLOCATIONS.md),
[DETERMINISM.md](DETERMINISM.md),
`AiHardLimits`.

## Budgets (hard ceilings)

| Budget | Limit |
|--------|------:|
| Agents | 64 |
| Planner nodes | 512 |
| Expansions / planner step | 64 |
| Planner steps / agent / tick | 8 |
| Plan length | 16 |
| Action candidates | 256 |
| Cover candidates | 32 |
| Perception candidates | 32 |
| Action ticks | 1024 |

Configuration may lower these for smaller scenarios.

## Cost model

Per tick (worst case, all agents planning):

```
O(agents × stepsPerTick × expansionsPerStep × candidatesExamined)
```

Keep candidate examined sets small via fact-indexed action lists (actions keyed
by effect bits).

## Allocation

Zero deliberate GC traffic on frozen path. Profile with server GC disabled noise
in mind; use SimulationTests allocation probes once Runtime exists
(`REQ-ALLOC-001`).

## Profiling guidance

1. Measure planner expansions counters from `PlannerResult`.
2. Track ring-buffer drop counts under load.
3. Avoid per-agent LINQ and string work on tick.
4. Host nav queries often dominate; budget them separately from planner.

## Scaling knobs

- Reduce `MaximumPlannerExpansionsPerStep` for weaker hosts.
- Stagger agents across ticks (still process in id order within a tick subset).
- Lower tactical point counts and cover candidate radius.

## Implementation status

Limits documented: **Implemented**. Runtime measurements: **Pending**.
