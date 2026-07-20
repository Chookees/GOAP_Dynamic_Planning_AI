# Requirements Traceability

Maps product requirements to documentation and code. Status values:

| Status | Meaning |
|--------|---------|
| `Implemented` | Present and usable |
| `PartiallyImplemented` | Contracts/docs or partial code exist |
| `Pending` | Specified; implementation not landed |

Related: [BACKLOG.md](BACKLOG.md), [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md),
[ARCHITECTURE.md](ARCHITECTURE.md).

---

## REQ-GOAP-001 — Backward regression GOAP planner

**Statement:** Provide an incremental backward-regression A* planner over symbolic
world facts with explicit budgets and `PlannerResult` / `PlannerStatus` outcomes.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/GOAP_PLANNER.md` | Spec + worked example | Implemented |
| `docs/WORLD_STATE_MODEL.md` | Fact model | Implemented |
| `docs/GOAL_ARBITRATION.md` | Goal selection | Implemented (docs) |
| `docs/ACTION_EXECUTION.md` | Execution lifecycle | Implemented (docs) |
| `src/TacticalGoap.Abstractions/Results/PlannerResult.cs` | Result contract | Implemented |
| `src/TacticalGoap.Abstractions/Results/PlannerStatus.cs` | Status enum | Implemented |
| `src/TacticalGoap.Abstractions/Enums/WorldFactId.cs` | Fact catalog | Implemented |
| `src/TacticalGoap.Abstractions/Limits/AiHardLimits.cs` | Planner caps | Implemented |
| `src/TacticalGoap.Runtime/**` (planner) | Implementation | Pending |
| `tests/TacticalGoap.UnitTests/**` | Planner tests | Pending |

**Overall:** PartiallyImplemented

---

## REQ-PO10-001 — Power-of-Ten compliance

**Statement:** Adapt and enforce Power-of-Ten rules on the frozen runtime path;
audit offline; build treats warnings as errors.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/POWER_OF_TEN_COMPLIANCE.md` | Rule map | Implemented |
| `src/TacticalGoap.Abstractions/Attributes/FrozenRuntimePathAttribute.cs` | Path marker | Implemented |
| `Directory.Build.props` | Analyzers, no unsafe, overflow | Implemented |
| `tools/TacticalGoap.Audit/**` | Source audit | PartiallyImplemented |
| Architecture / unit tests for bounds | Enforcement | Pending |

**Overall:** PartiallyImplemented

---

## REQ-ALLOC-001 — Bounded allocations after freeze

**Statement:** No deliberate managed allocation on frozen tick path; fixed
capacities via `AiHardLimits`.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/MEMORY_AND_ALLOCATIONS.md` | Policy | Implemented |
| `AiHardLimits.cs` | Ceilings | Implemented |
| `RuntimeLifecycleState.cs` | Freeze states | Implemented |
| Runtime buffer pools | Implementation | Pending |
| Allocation probes in tests | Verification | Pending |

**Overall:** PartiallyImplemented

---

## REQ-DET-001 — Deterministic ticks

**Statement:** Identical tick sequences and host inputs yield identical decisions;
no wall-clock dependency.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/DETERMINISM.md` | Rules | Implemented |
| `src/TacticalGoap.Abstractions/Ticks/AiTick.cs` | Tick stamp | Implemented |
| Runtime ordering + RNG policy | Implementation | Pending |
| `tests/TacticalGoap.SimulationTests/**` | Replay digests | Pending |

**Overall:** PartiallyImplemented

---

## REQ-SQUAD-001 — Squad coordination

**Statement:** Support squads with one active behavior and discrete orders that
bias member goals without replacing individual planners.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/SQUAD_COORDINATION.md` | Spec | Implemented |
| `docs/COMMUNICATION_SYSTEM.md` | Intents | Implemented |
| `DomainEnums.cs` (`SquadOrderType`, `SquadBehaviorType`) | Contracts | Implemented |
| `AiHardLimits` squad caps | Limits | Implemented |
| Runtime squad coordinator | Implementation | Pending |

**Overall:** PartiallyImplemented

---

## REQ-COVER-001 — Tactical points, cover, emergent flanking

**Statement:** Score and reserve tactical points; demonstrate emergent side-attack
/ flanking via cover weights + GOAP (no scripted Flank action required).

| Artifact | Role | Status |
|----------|------|--------|
| `docs/TACTICAL_POINTS_AND_COVER.md` | Spec + demo explanation | Implemented |
| `TacticalPointCategory` / ids | Contracts | Implemented |
| Cover scorer + reservations | Implementation | Pending |
| Sample flanking scenario + simulation tests | Demo | Pending |

**Overall:** PartiallyImplemented

---

## REQ-NAV-001 — Navigation host integration

**Statement:** Orchestrate movement through host navigation services; mirror path
validity into world facts and failure reasons.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/NAVIGATION_INTEGRATION.md` | Spec | Implemented |
| `docs/ENGINE_INTEGRATION_GUIDE.md` | Hosting | Implemented |
| Navigation identifiers | Contracts | Implemented |
| Host interfaces + executor wiring | Implementation | Pending |

**Overall:** PartiallyImplemented

---

## REQ-DOC-001 — Documentation and fidelity notices

**Statement:** Maintain complete Markdown docs, MIT license, NOTICE with
originality / non-reverse-engineering statement, and this traceability matrix.

| Artifact | Role | Status |
|----------|------|--------|
| `README.md` | Entry + commands | Implemented |
| `LICENSE.md` | MIT | Implemented |
| `NOTICE.md` | Third-party + inspiration | Implemented |
| `docs/**` | Technical set | Implemented |
| `docs/PUBLIC_SOURCE_FIDELITY.md` | Fidelity policy | Implemented |
| `docs/BACKLOG.md` | Backlog | Implemented |
| This file | Traceability | Implemented |

**Overall:** Implemented

---

## REQ-SAMPLE-001 — Deterministic sample scenarios

**Statement:** Ship a console sample runnable with `--scenario BasicAttack` (and
related demos) for CI and developer onboarding.

| Artifact | Role | Status |
|----------|------|--------|
| `README.md` sample command | Docs | Implemented |
| `src/TacticalGoap.Sample/**` | Console host | PartiallyImplemented |
| Scenario assets / BasicAttack | Implementation | Pending |
| Simulation tests | Verification | Pending |

**Overall:** PartiallyImplemented

---

## REQ-PUSH-001 — Linear integration on feature branch

**Statement:** Land work on `cursor/tactical-goap-framework-fe97` with
cherry-pick/rebase linear history (ADR-003); push documentation and code via
agreed commit messages.

| Artifact | Role | Status |
|----------|------|--------|
| `docs/DESIGN_DECISIONS.md` ADR-003 | Policy | Implemented |
| Git branch workflow | Process | Implemented (process) |
| Agent push of docs commit `TG-DOC-001` | Execution | In progress with this change set |

**Overall:** PartiallyImplemented (process ongoing)

---

## Summary matrix

| Req | Overall status |
|-----|----------------|
| REQ-GOAP-001 | PartiallyImplemented |
| REQ-PO10-001 | PartiallyImplemented |
| REQ-ALLOC-001 | PartiallyImplemented |
| REQ-DET-001 | PartiallyImplemented |
| REQ-SQUAD-001 | PartiallyImplemented |
| REQ-COVER-001 | PartiallyImplemented |
| REQ-NAV-001 | PartiallyImplemented |
| REQ-DOC-001 | Implemented |
| REQ-SAMPLE-001 | PartiallyImplemented |
| REQ-PUSH-001 | PartiallyImplemented |
