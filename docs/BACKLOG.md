# Backlog

Structured backlog for TacticalGoap. Task IDs are stable; update **Status** as
work lands. Priorities: `P0` (blocking), `P1` (core), `P2` (important),
`P3` (later).

Related: [REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md),
[DESIGN_DECISIONS.md](DESIGN_DECISIONS.md).

Status values: `Done`, `InProgress`, `Todo`, `Blocked`.

---

## TG-DOC — Documentation

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-DOC-001 | Architecture docs, backlog, requirements traceability | P0 | Done | REQ-DOC-001 |
| TG-DOC-002 | Keep docs in sync as Runtime APIs freeze | P2 | Todo | REQ-DOC-001 |
| TG-DOC-003 | Add XML-doc → markdown extract optional tooling note | P3 | Todo | REQ-DOC-001 |

---

## TG-ARC — Architecture & solution

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-ARC-001 | Enforce project reference direction via ArchitectureTests | P0 | Todo | REQ-PUSH-001 |
| TG-ARC-002 | Runtime lifecycle state machine implementation | P0 | Todo | REQ-ALLOC-001 |
| TG-ARC-003 | Agent registry within MaximumAgents | P0 | Todo | REQ-GOAP-001 |
| TG-ARC-004 | Tick entrypoint validating AiTick | P0 | Todo | REQ-DET-001 |
| TG-ARC-005 | Solution-wide `dotnet format` baseline | P1 | Todo | REQ-PO10-001 |

---

## TG-ABS — Abstractions completion

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-ABS-001 | World-state bitmask helpers | P0 | Todo | REQ-GOAP-001 |
| TG-ABS-002 | Host service interface contracts (nav, weapon, anim) | P0 | Todo | REQ-NAV-001 |
| TG-ABS-003 | Action definition / candidate structs | P0 | Todo | REQ-GOAP-001 |
| TG-ABS-004 | Goal definition structs | P0 | Todo | REQ-GOAP-001 |
| TG-ABS-005 | Diagnostic record struct layout | P1 | Todo | REQ-DOC-001 |

*Note: Identifiers, enums, limits, planner result types already landed.*

---

## TG-PLN — Planner

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-PLN-001 | Fixed-capacity open-set binary heap | P0 | Todo | REQ-GOAP-001 |
| TG-PLN-002 | Planner node pool + closed set | P0 | Todo | REQ-GOAP-001 |
| TG-PLN-003 | Backward regression expand step | P0 | Todo | REQ-GOAP-001 |
| TG-PLN-004 | Plan reconstruction ≤ MaximumPlanLength | P0 | Todo | REQ-GOAP-001 |
| TG-PLN-005 | Incremental step budgets + PlannerResult | P0 | Todo | REQ-GOAP-001 |
| TG-PLN-006 | Effect-bit action index for candidate lookup | P1 | Todo | REQ-GOAP-001 |
| TG-PLN-007 | Unit tests for BasicAttack worked example | P0 | Todo | REQ-GOAP-001 |

---

## TG-GOAL — Goals & arbitration

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-GOAL-001 | Goal registration tables | P0 | Todo | REQ-GOAP-001 |
| TG-GOAL-002 | Per-tick arbitrator with stable tie-break | P0 | Todo | REQ-GOAP-001 |
| TG-GOAL-003 | Interrupt + replan policies | P1 | Todo | REQ-GOAP-001 |
| TG-GOAL-004 | Squad bias hooks | P1 | Todo | REQ-SQUAD-001 |

---

## TG-ACT — Action execution

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-ACT-001 | Action executor state machine | P0 | Todo | REQ-GOAP-001 |
| TG-ACT-002 | Volatile precondition checks | P0 | Todo | REQ-GOAP-001 |
| TG-ACT-003 | Failure reason mapping + reservation release | P0 | Todo | REQ-GOAP-001 |
| TG-ACT-004 | Replanning attempt counter | P1 | Todo | REQ-GOAP-001 |
| TG-ACT-005 | Built-in Move / Reload / Attack / TakeCover handlers | P0 | Todo | REQ-SAMPLE-001 |

---

## TG-MEM — Perception & memory

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-MEM-001 | Working-memory store (64 records) | P0 | Todo | REQ-GOAP-001 |
| TG-MEM-002 | Ingress buffer drain | P0 | Todo | REQ-DET-001 |
| TG-MEM-003 | TTL expiry | P1 | Todo | REQ-GOAP-001 |
| TG-MEM-004 | Quantizers → WorldFactId | P0 | Todo | REQ-GOAP-001 |

---

## TG-TGT — Target & weapon

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-TGT-001 | Target scoring + hysteresis | P1 | Todo | REQ-GOAP-001 |
| TG-TGT-002 | Weapon selection by range band | P1 | Todo | REQ-GOAP-001 |
| TG-TGT-003 | Candidate binding to focus ids | P1 | Todo | REQ-GOAP-001 |

---

## TG-COV — Cover & tactical points

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-COV-001 | Tactical point table | P0 | Todo | REQ-COVER-001 |
| TG-COV-002 | Cover scorer with flank-angle weight | P0 | Todo | REQ-COVER-001 |
| TG-COV-003 | Reservation table | P0 | Todo | REQ-COVER-001 |
| TG-COV-004 | Flanking demo scenario data | P1 | Todo | REQ-COVER-001 |
| TG-COV-005 | Simulation test asserting side-point selection | P1 | Todo | REQ-COVER-001 |

---

## TG-NAV — Navigation

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-NAV-001 | INavigationHost interface | P0 | Todo | REQ-NAV-001 |
| TG-NAV-002 | Path fact mirroring (PathValid, arrival) | P0 | Todo | REQ-NAV-001 |
| TG-NAV-003 | Sample grid/waypoint nav host | P1 | Todo | REQ-SAMPLE-001 |
| TG-NAV-004 | Traversal link actions (window/vault) | P2 | Todo | REQ-NAV-001 |

---

## TG-SQD — Squad

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-SQD-001 | Squad registry + membership | P1 | Todo | REQ-SQUAD-001 |
| TG-SQD-002 | Behavior selection | P1 | Todo | REQ-SQUAD-001 |
| TG-SQD-003 | Order allocation / expiry | P1 | Todo | REQ-SQUAD-001 |
| TG-SQD-004 | GetToCover + AdvanceCover behaviors | P1 | Todo | REQ-SQUAD-001 |
| TG-SQD-005 | Search sector assignment | P2 | Todo | REQ-SQUAD-001 |

---

## TG-COM — Communication

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-COM-001 | Intent request queue | P2 | Todo | REQ-SQUAD-001 |
| TG-COM-002 | Emission arbitrator | P2 | Todo | REQ-SQUAD-001 |
| TG-COM-003 | Delivery → memory upsert | P2 | Todo | REQ-SQUAD-001 |

---

## TG-CFG — Configuration

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-CFG-001 | JSON schema / DTOs | P1 | Todo | REQ-SAMPLE-001 |
| TG-CFG-002 | Validator against AiHardLimits | P1 | Todo | REQ-ALLOC-001 |
| TG-CFG-003 | Builder API for code-first hosts | P1 | Todo | REQ-NAV-001 |

---

## TG-DIA — Diagnostics

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-DIA-001 | Ring buffer writer (frozen-safe) | P1 | Todo | REQ-DET-001 |
| TG-DIA-002 | On-demand formatters in Diagnostics assembly | P1 | Todo | REQ-DOC-001 |
| TG-DIA-003 | Contract assertion helpers | P2 | Todo | REQ-PO10-001 |

---

## TG-AUD — Audit tool

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-AUD-001 | Roslyn walk of production projects | P1 | Todo | REQ-PO10-001 |
| TG-AUD-002 | Detect allocations in FrozenRuntimePath | P1 | Todo | REQ-ALLOC-001 |
| TG-AUD-003 | Method logical line limit (60) | P1 | Todo | REQ-PO10-001 |
| TG-AUD-004 | Ban unsafe / Runtime #if forks | P2 | Todo | REQ-PO10-001 |

---

## TG-SMP — Sample

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-SMP-001 | CLI `--scenario` parsing | P0 | Todo | REQ-SAMPLE-001 |
| TG-SMP-002 | BasicAttack scenario | P0 | Todo | REQ-SAMPLE-001 |
| TG-SMP-003 | FlankCover demo scenario | P1 | Todo | REQ-COVER-001 |
| TG-SMP-004 | Deterministic digest stdout | P1 | Todo | REQ-DET-001 |

---

## TG-TST — Tests

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-TST-001 | Unit planner suite | P0 | Todo | REQ-GOAP-001 |
| TG-TST-002 | Lifecycle integration suite | P0 | Todo | REQ-ALLOC-001 |
| TG-TST-003 | Determinism simulation suite | P0 | Todo | REQ-DET-001 |
| TG-TST-004 | Architecture dependency suite | P0 | Todo | REQ-PUSH-001 |
| TG-TST-005 | Cover flanking simulation | P1 | Todo | REQ-COVER-001 |

---

## TG-PERF — Performance

| ID | Title | Priority | Status | Req |
|----|-------|----------|--------|-----|
| TG-PERF-001 | Expansion counters in sample digest | P2 | Todo | REQ-GOAP-001 |
| TG-PERF-002 | Allocation probe helper for frozen path | P1 | Todo | REQ-ALLOC-001 |
| TG-PERF-003 | Document host nav budget separately | P3 | Todo | REQ-NAV-001 |

---

## Suggested implementation order

1. TG-ABS-001…004, TG-ARC-002…004  
2. TG-PLN-001…005, TG-ACT-001…003  
3. TG-MEM-*, TG-GOAL-*  
4. TG-COV-*, TG-NAV-*  
5. TG-SMP-001…002, TG-TST-*  
6. TG-SQD-*, TG-COM-*, TG-AUD-*, TG-CFG-*
