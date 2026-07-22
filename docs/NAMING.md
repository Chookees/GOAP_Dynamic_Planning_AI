# Naming — DynamicPlanningAI (DP_AI)

This document is the authoritative naming guide for the product.

## Canonical names

| Form | Use |
|------|-----|
| **DynamicPlanningAI** | Product name, solution name (`DynamicPlanningAI.sln`), root namespaces, assembly names, copyright holders, documentation titles |
| **DP_AI** | Short form of DynamicPlanningAI in prose, diagrams, and UI-facing labels where space is limited |
| **DP** | Abbreviation of **DynamicPlanning** only — never a standalone product name |

**DP always means DynamicPlanning.**

Examples:

- Correct: “DP_AI (DynamicPlanningAI)”
- Correct: “DP-PLN-001 — planner task under DynamicPlanning”
- Incorrect: referring to the product only as “DP” without expanding DynamicPlanning nearby on first use in a document

## Project and namespace map

| Project | Root namespace |
|---------|----------------|
| `DynamicPlanningAI.Abstractions` | `DynamicPlanningAI.Abstractions` |
| `DynamicPlanningAI.Runtime` | `DynamicPlanningAI.Runtime` |
| `DynamicPlanningAI.Configuration` | `DynamicPlanningAI.Configuration` |
| `DynamicPlanningAI.Diagnostics` | `DynamicPlanningAI.Diagnostics` |
| `DynamicPlanningAI.Sample` | `DynamicPlanningAI.Sample` |
| `DynamicPlanningAI.Audit` | `DynamicPlanningAI.Audit` |
| `DynamicPlanningAI.UnitTests` | `DynamicPlanningAI.UnitTests` |
| `DynamicPlanningAI.IntegrationTests` | `DynamicPlanningAI.IntegrationTests` |
| `DynamicPlanningAI.ArchitectureTests` | `DynamicPlanningAI.ArchitectureTests` |
| `DynamicPlanningAI.SimulationTests` | `DynamicPlanningAI.SimulationTests` |

## Task ID prefix

Backlog and commit titles use:

```text
DP-<AREA>-<nnn>
```

Where **DP** = **DynamicPlanning**. Area codes include `ABS`, `PLN`, `ACT`,
`MEM`, `COV`, `SQD`, `DIA`, `CFG`, `SIM`, `TST`, `DOC`, `FIX`, and others listed
in [BACKLOG.md](BACKLOG.md).

## Domain terms that are not product names

These remain generic AI / GOAP vocabulary and are **not** renamed to DP_AI:

- `AiRuntime`, `AiTick`, `AiHardLimits`, `AiContract`
- GOAP, planner, world state, squad, cover, and similar domain words

## Historical note

Earlier drafts used the working name “TacticalGoap” / `TG-*` task IDs. The
product is now **DynamicPlanningAI (DP_AI)** throughout the repository.
