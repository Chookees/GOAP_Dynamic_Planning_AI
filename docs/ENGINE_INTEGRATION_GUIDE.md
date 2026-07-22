# Engine Integration Guide

DynamicPlanningAI is **engine-agnostic**. Integrate by implementing host services and
driving `AiTick`. This guide is not Unity-specific; Unity, Unreal, Godot, custom
C++/C# engines, and the console Sample all use the same pattern.

Related: [ARCHITECTURE.md](ARCHITECTURE.md),
[NAVIGATION_INTEGRATION.md](NAVIGATION_INTEGRATION.md),
[ACTION_EXECUTION.md](ACTION_EXECUTION.md),
[DETERMINISM.md](DETERMINISM.md),
[CONFIGURATION.md](CONFIGURATION.md).

## Integration steps

1. **Reference packages/projects** — at minimum Abstractions + Runtime;
   Configuration optional if you build definitions in code; Diagnostics optional.
2. **Construct runtime** → `Configuring`.
3. **Register** agents, goals, actions, tactical data (or load via Configuration).
4. **Initialize** buffers → `Initialized`.
5. **Freeze** → `Frozen`.
6. **Start** → `Running`.
7. Each simulation step: push perception/damage events, call `Tick(AiTick)`,
   read animation/weapon/navigation requests, apply in the engine, write results
   back next tick.
8. **Stop** / **Dispose** on teardown.

## Host services to implement

| Service | Responsibility |
|---------|----------------|
| Navigation | Find/follow/invalidate paths |
| Animation | Accept posture / fire / reload requests |
| Weapon | Apply shots, report ammo |
| Interaction | Doors, smart objects |
| Spatial query | Nearby tactical points / agents (or supply precomputed) |

Map host rejections to `OperationStatus.HostRejected` and action failure
reasons (`AnimationRejected`, `WeaponRejected`, `InteractionRejected`).

## Tick ownership

Choose a deterministic cadence (fixed update, AI interval). Build
`AiTick` with a monotonic sequence. Do not call tick from multiple threads.

```text
Engine FixedStep N
  → gather sensors
  → runtime.Tick(new AiTick(N, dtMs))
  → apply movement/animation requests
  → defer async path results to step N+k as explicit inputs
```

## Adapter sketches

### Console / headless (Sample)

In-process grid nav and instant hit resolution; ideal for CI.

### Unity

- Drive from `FixedUpdate` or a custom AI player loop.
- Wrap `NavMeshAgent` / `NavMesh.CalculatePath`.
- Keep managed allocations in the adapter, not inside Runtime frozen methods.

### Unreal

- C# via plugin or mirror intents through a C API boundary if Runtime stays in
  .NET; alternatively rehost contracts—still treat Unreal as the host.
- Use AI tick / EQS results as perception ingress.

### Godot

- `_PhysicsProcess` as tick source.
- `NavigationAgent3D` as nav host.

## What not to do

- Do not call into engine singletons from `DynamicPlanningAI.Runtime`.
- Do not pass `GameObject` / `AActor` references into world state.
- Do not rely on coroutines that complete off-tick without recording completion
  on a future `AiTick`.

## Sample command

```bash
dotnet run --project src/DynamicPlanningAI.Sample --configuration Release -- --scenario BasicAttack
```

## Implementation status

Guide: **Implemented** (docs). Host interfaces in code: **Pending**
(`REQ-PUSH-001` tracks integration branch strategy; hosting APIs under Runtime
backlog).
