# Backlog

Structured backlog for **DynamicPlanningAI (DP_AI)**. Task IDs are stable.

**DP** in every `DP-*` task identifier means **DynamicPlanning**.

Status values: `Done`, `InProgress`, `Todo`, `Blocked`.

Related: [NAMING.md](NAMING.md), [REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md),
[DESIGN_DECISIONS.md](DESIGN_DECISIONS.md).

---

## Completed milestone tasks

| ID | Title | Status | Commit theme |
|----|-------|--------|--------------|
| DP-DOC-001 | Architecture docs, backlog, requirements | Done | DP-DOC-001 |
| DP-ABS-001 | Core abstractions, host interfaces, contracts | Done | DP-ABS-001 |
| DP-PLN-001 | Symbolic world state + bounded GOAP planner | Done | DP-PLN-001 |
| DP-MEM-001 | Working memory, perception, focus selectors | Done | DP-MEM-001 |
| DP-SQD-001 | Squad coordinator + communication | Done | DP-SQD-001 |
| DP-COV-001 | Cover, reservations, navigation orchestration | Done | DP-COV-001 |
| DP-ACT-001 | Goals, actions, plan execution, AiRuntime | Done | DP-ACT-001 |
| DP-DIA-001 | Diagnostics formatters, ring buffer, audit tool | Done | DP-DIA-001 |
| DP-CFG-001 | Validated immutable configuration groups | Done | DP-CFG-001 |
| DP-SIM-001 | Deterministic sample simulation + scenarios | Done | DP-SIM-001 |
| DP-TST-001 | Integration and simulation scenario tests | Done | DP-TST-001 |
| DP-ARC-001 | Architecture tests (layering, unsafe, LINQ) | Done | DP-DIA-001 |
| DP-FIX-001 | Format baseline + CA1515 test hardening | Done | DP-FIX-001 |

---

## Follow-ups (optional / non-blocking)

| ID | Title | Priority | Status |
|----|-------|----------|--------|
| DP-DOC-002 | Refresh worked examples against live API signatures | P2 | Todo |
| DP-AUD-002 | Reduce POT008/010/013 heuristic false positives | P2 | Todo |
| DP-PERF-001 | Expand performance harness metrics export | P3 | Todo |
| DP-ACT-002 | Deeper host-polled animation completion paths | P2 | Todo |
| DP-INT-001 | Additional ECS/OO engine adapter samples | P3 | Todo |

---

## Integration branch

- Branch: `cursor/rename-dynamic-planning-ai-fe97`
- Strategy: linear atomic commits on the integration branch (ADR in DESIGN_DECISIONS.md)
- Default branch `main` remains stable until PR merge
