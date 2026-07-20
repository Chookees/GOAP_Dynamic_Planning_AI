# Requirements Traceability

Maps product requirements to documentation, implementation, and tests.

| Status | Meaning |
|--------|---------|
| `Implemented` | Present, built, and covered by automated verification |
| `PartiallyImplemented` | Core present; residual gaps documented |
| `Pending` | Not started |

Related: [BACKLOG.md](BACKLOG.md), [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md),
[ARCHITECTURE.md](ARCHITECTURE.md).

---

## REQ-GOAP-001 — Backward regression GOAP planner

**Statement:** Incremental backward-regression A* over symbolic world facts with
explicit budgets and `PlannerResult` / `PlannerStatus` outcomes.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/GOAP_PLANNER.md` | Spec + worked example | Implemented |
| `src/TacticalGoap.Runtime/WorldState/*` | Symbolic state | Implemented |
| `src/TacticalGoap.Runtime/Planning/*` | Planner, heap, workspace | Implemented |
| `src/TacticalGoap.Runtime/Goals/*` | Goals + arbitration | Implemented |
| `src/TacticalGoap.Runtime/Actions/*` | Action catalog + executors | Implemented |
| `tests/TacticalGoap.UnitTests/Planning/*` | Planner tests | Implemented |
| `tests/TacticalGoap.UnitTests/WorldState/*` | World-state tests | Implemented |

**Overall:** Implemented

---

## REQ-PO10-001 — Power-of-Ten compliance

| Artifact | Role | Status |
|----------|------|--------|
| `docs/POWER_OF_TEN_COMPLIANCE.md` | Rule map | Implemented |
| `FrozenRuntimePathAttribute` | Path marker | Implemented |
| `Directory.Build.props` | Analyzers, overflow, no unsafe | Implemented |
| `tools/TacticalGoap.Audit` | Source audit POT001–POT013 | Implemented |
| `tests/TacticalGoap.ArchitectureTests` | Layering / LINQ / unsafe | Implemented |

**Overall:** Implemented (audit warns on heuristics; zero errors required)

---

## REQ-ALLOC-001 — Bounded allocations after freeze

| Artifact | Role | Status |
|----------|------|--------|
| `docs/MEMORY_AND_ALLOCATIONS.md` | Policy | Implemented |
| Fixed-capacity buffers across Runtime | Implementation | Implemented |
| `ZeroAllocationSteadyState` scenario | Measurement | Implemented |
| `AllocationProbe` | Test helper | Implemented |

**Overall:** Implemented

---

## REQ-DET-001 — Deterministic ticks

| Artifact | Role | Status |
|----------|------|--------|
| `docs/DETERMINISM.md` | Rules | Implemented |
| `AiTick` + host-driven sequence | Tick stamp | Implemented |
| `DeterministicRandom` | Seeded PRNG | Implemented |
| Integration deterministic replay tests | Verification | Implemented |

**Overall:** Implemented

---

## REQ-SQUAD-001 — Squad coordination

| Artifact | Role | Status |
|----------|------|--------|
| `docs/SQUAD_COORDINATION.md` | Spec | Implemented |
| `src/TacticalGoap.Runtime/Squad/*` | Coordinator + behaviors | Implemented |
| `tests/TacticalGoap.UnitTests/Squad/*` | Unit tests | Implemented |
| Sample `SquadSearch`, `AdvanceUnderSuppression` | Scenarios | Implemented |

**Overall:** Implemented

---

## REQ-COVER-001 — Cover and reservations

| Artifact | Role | Status |
|----------|------|--------|
| `docs/TACTICAL_POINTS_AND_COVER.md` | Spec + emergent flank | Implemented |
| `src/TacticalGoap.Runtime/Cover/*` | Scoring + invalidation | Implemented |
| `src/TacticalGoap.Runtime/Reservations/*` | Reservation table | Implemented |
| Cover unit tests + OrderOverriddenByDanger | Verification | Implemented |

**Overall:** Implemented

---

## REQ-NAV-001 — Navigation orchestration

| Artifact | Role | Status |
|----------|------|--------|
| `docs/NAVIGATION_INTEGRATION.md` | Spec | Implemented |
| `INavigationService` + orchestrator | Contracts + runtime | Implemented |
| `GridNavigationService` (Sample) | Bounded grid adapter | Implemented |
| BlockedDoor / WindowTraversal scenarios | Verification | Implemented |

**Overall:** Implemented

---

## REQ-DOC-001 — Documentation completeness

| Artifact | Role | Status |
|----------|------|--------|
| All `docs/*.md` listed in solution structure | Markdown set | Implemented |
| XML docs on production assemblies | CS1591 as error | Implemented |
| README / LICENSE / NOTICE | Root docs | Implemented |

**Overall:** Implemented

---

## REQ-SAMPLE-001 — Deterministic sample simulation

| Artifact | Role | Status |
|----------|------|--------|
| `src/TacticalGoap.Sample` | Console sim + 12 scenarios | Implemented |
| `tests/TacticalGoap.SimulationTests` | Smoke tests | Implemented |
| `tests/TacticalGoap.IntegrationTests` | Door replan, override, replay | Implemented |

**Overall:** Implemented

---

## REQ-PUSH-001 — Atomic commits and remote push

| Artifact | Role | Status |
|----------|------|--------|
| Integration branch `cursor/tactical-goap-framework-fe97` | Workflow | Implemented |
| Atomic TG-* commits pushed to origin | History | Implemented |

**Overall:** Implemented

---

## Verification commands

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet format --verify-no-changes
dotnet run --project tools/TacticalGoap.Audit --configuration Release
dotnet run --project src/TacticalGoap.Sample --configuration Release -- --scenario BasicAttack
```
