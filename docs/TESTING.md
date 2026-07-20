# Testing

Test projects validate contracts, determinism, planning, and architecture
boundaries.

Related: [DETERMINISM.md](DETERMINISM.md),
[POWER_OF_TEN_COMPLIANCE.md](POWER_OF_TEN_COMPLIANCE.md),
[GOAP_PLANNER.md](GOAP_PLANNER.md),
[REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md).

## Projects

| Project | Focus |
|---------|-------|
| `TacticalGoap.UnitTests` | Masks, heap, planner steps, scoring pure functions |
| `TacticalGoap.IntegrationTests` | Runtime lifecycle, config load, host fakes |
| `TacticalGoap.SimulationTests` | Multi-tick scenarios, flanking demo, determinism digests |
| `TacticalGoap.ArchitectureTests` | Project reference direction, no illegal deps |

## Commands

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet format --verify-no-changes
dotnet run --project tools/TacticalGoap.Audit --configuration Release
```

## Strategies

### Unit

- Planner worked example from [GOAP_PLANNER.md](GOAP_PLANNER.md)
- Heap ordering tie-breaks
- World-fact mask set/clear
- Capacity overflow statuses

### Integration

- Lifecycle illegal transitions → `InvalidLifecycleState`
- Freeze then mutate → rejected
- Fake nav host returning NoPath → action failure + replan

### Simulation

- `BasicAttack` reaches attack success under scripted host replies
- Flanking layout selects side cover (`REQ-COVER-001`)
- Two runs same seed → identical diagnostic digest (`REQ-DET-001`)

### Architecture

- Runtime must not reference Configuration/Sample/Diagnostics
- Diagnostics must not reference Runtime
- Dependency direction matches [ARCHITECTURE.md](ARCHITECTURE.md)

## Naming

Map tests to requirement ids in method names or traits where practical, e.g.
`Planner_BasicAttack_REQ_GOAP_001`.

## Implementation status

Test projects scaffolded: **PartiallyImplemented**. Suites: **Pending**
(`REQ-SAMPLE-001` and others).
