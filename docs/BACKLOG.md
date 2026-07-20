# Backlog

Structured backlog for TacticalGoap. Task IDs are stable.

Status values: `Done`, `InProgress`, `Todo`, `Blocked`.

Related: [REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md),
[DESIGN_DECISIONS.md](DESIGN_DECISIONS.md).

---

## Completed milestone tasks

| ID | Title | Status | Commit theme |
|----|-------|--------|--------------|
| TG-DOC-001 | Architecture docs, backlog, requirements | Done | TG-DOC-001 |
| TG-ABS-001 | Core abstractions, host interfaces, contracts | Done | TG-ABS-001 |
| TG-PLN-001 | Symbolic world state + bounded GOAP planner | Done | TG-PLN-001 |
| TG-MEM-001 | Working memory, perception, focus selectors | Done | TG-MEM-001 |
| TG-SQD-001 | Squad coordinator + communication | Done | TG-SQD-001 |
| TG-COV-001 | Cover, reservations, navigation orchestration | Done | TG-COV-001 |
| TG-ACT-001 | Goals, actions, plan execution, AiRuntime | Done | TG-ACT-001 |
| TG-DIA-001 | Diagnostics formatters, ring buffer, audit tool | Done | TG-DIA-001 |
| TG-CFG-001 | Validated immutable configuration groups | Done | TG-CFG-001 |
| TG-SIM-001 | Deterministic sample simulation + scenarios | Done | TG-SIM-001 |
| TG-TST-001 | Integration and simulation scenario tests | Done | TG-TST-001 |
| TG-ARC-001 | Architecture tests (layering, unsafe, LINQ) | Done | TG-DIA-001 |
| TG-FIX-001 | Format baseline + CA1515 test hardening | Done | TG-FIX-001 |

---

## Follow-ups (optional / non-blocking)

| ID | Title | Priority | Status |
|----|-------|----------|--------|
| TG-DOC-002 | Refresh worked examples against live API signatures | P2 | Todo |
| TG-AUD-002 | Reduce POT008/010/013 heuristic false positives | P2 | Todo |
| TG-PERF-001 | Expand performance harness metrics export | P3 | Todo |
| TG-ACT-002 | Deeper host-polled animation completion paths | P2 | Todo |
| TG-INT-001 | Additional ECS/OO engine adapter samples | P3 | Todo |

---

## Integration branch

- Branch: `cursor/tactical-goap-framework-fe97`
- Strategy: linear atomic commits on the integration branch (ADR in DESIGN_DECISIONS.md)
- Default branch `main` remains stable until PR merge
