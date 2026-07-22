# Public Source Fidelity

This document records how DynamicPlanningAI relates to publicly discussed tactical GOAP
ideas (including F.E.A.R.-era industry presentations) and what it deliberately
does **not** claim.

Related: [NOTICE.md](../NOTICE.md), [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md),
[README.md](../README.md).

## Statement of originality

DynamicPlanningAI is **original software**. Contributors did not reverse-engineer,
decompile, or copy proprietary game AI binaries, scripts, or confidential
materials. Types, algorithms, capacity tables, APIs, and documentation are
engineered for this repository’s requirements: determinism, bounded memory,
Power-of-Ten discipline, and engine-agnostic hosting.

## What “inspired by” means

Public materials describe recurring **principles**, for example:

| Public principle | DynamicPlanningAI expression |
|------------------|-------------------------|
| Agents plan toward goals rather than hard-scripting every tactic | GOAP planner over goal desire conditions |
| World knowledge is a compact symbolic state | `WorldFactId` bit-mask (≤ 64 facts) |
| Sensors write working memory, not the planner directly | Perception → `MemoryType` records → quantized facts |
| Goals compete; highest utility / priority wins | Goal arbitration per tick |
| Actions have preconditions and effects | Immutable action definitions + candidates |
| Squads add coordination without replacing individual planners | Squad orders as world facts and goal bias |
| Cover and flanking emerge from scoring + planning | Tactical points, reservations, regression planning |

These are industry-standard AI architecture ideas, not proprietary trade secrets.

## What DynamicPlanningAI does not include

- No proprietary action graphs, animation trees, or dialogue banks
- No binary-compatible layouts matching any commercial engine
- No ripped assets, maps, or voice lines
- No claim of behavioral parity with any shipped title
- No use of confidential postmortem slides beyond concepts already public

## Design divergence (intentional)

DynamicPlanningAI makes engineering choices that differ from historical descriptions:

1. **Backward regression A\*** with explicit incremental budgets and typed
   `PlannerStatus` outcomes (see [GOAP_PLANNER.md](GOAP_PLANNER.md), ADR-001).
2. **Custom open-set heap** instead of `PriorityQueue<T>` for determinism and
   capacity control (ADR-002).
3. **Hard capacity ceilings** in `AiHardLimits` with freeze-time allocation only.
4. **Engine-agnostic host contracts** rather than embedding a specific engine.
5. **Power-of-Ten C# adaptations** enforced by audit tooling
   ([POWER_OF_TEN_COMPLIANCE.md](POWER_OF_TEN_COMPLIANCE.md)).
6. **Semantic communication intents** as first-class enums, not free-form strings.

## Citation practice

When documentation mentions F.E.A.R. or similar titles, it does so for
**educational context**: naming a well-known public example of GOAP in games.
Such mentions are not endorsements and do not assert derivation.

## Review checklist for contributors

Before merging AI behavior changes, confirm:

- [ ] No proprietary code, dumps, or assets were introduced
- [ ] New algorithms are justified in ADRs or subsystem docs
- [ ] Public inspiration is described at principle level only
- [ ] [NOTICE.md](../NOTICE.md) remains accurate

## Status

Fidelity policy is **Implemented** in documentation. Ongoing compliance is a
process requirement (`REQ-DOC-001`).
