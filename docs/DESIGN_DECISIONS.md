# Design Decisions (ADRs)

Architecture Decision Records for TacticalGoap. Newest decisions append with the
next ADR number.

Related: [ARCHITECTURE.md](ARCHITECTURE.md),
[PUBLIC_SOURCE_FIDELITY.md](PUBLIC_SOURCE_FIDELITY.md),
[GOAP_PLANNER.md](GOAP_PLANNER.md),
[POWER_OF_TEN_COMPLIANCE.md](POWER_OF_TEN_COMPLIANCE.md).

---

## ADR-001: Backward regression A* planner

**Status:** Accepted  
**Date:** 2026-07-20

### Context

GOAP planners may search forward from the current state or regress backward from
goal conditions. Tactical action libraries are large; goals are sparse bit
masks.

### Decision

Use **backward regression A\*** with incremental expansion budgets and explicit
`PlannerStatus` / `PlannerResult` outcomes.

### Consequences

- Lower branching when effects are indexed by fact bit
- Natural fit for “achieve these facts” goals
- Requires careful handling of action delete lists / false conditions
- Documented worked examples become the teaching tool for contributors

---

## ADR-002: Custom open-set heap (not `PriorityQueue<T>`)

**Status:** Accepted  
**Date:** 2026-07-20

### Context

.NET `PriorityQueue<TElement,TPriority>` is convenient but complicates strict
capacity caps, deterministic tie-breaking, and freeze-time pooled storage
without hidden growth.

### Decision

Implement a **fixed-capacity binary heap** over preallocated arrays inside the
planner workspace, with explicit overflow → `OpenSetCapacityExceeded`.

### Consequences

- Full control of memory and ordering
- Extra code to maintain and test
- No dependency on PriorityQueue enumeration/growth semantics

---

## ADR-003: Integration branch strategy (cherry-pick / linear history)

**Status:** Accepted  
**Date:** 2026-07-20

### Context

Cloud agents and parallel workstreams land on
`cursor/tactical-goap-framework-fe97`. Merge noise and divergent histories make
review and bisect harder.

### Decision

Integrate with a **linear history** on `cursor/tactical-goap-framework-fe97`
using rebase and/or cherry-pick of completed task commits. Avoid merge commits
for routine integrations when possible. Requirement id: `REQ-PUSH-001`.

### Consequences

- Cleaner bisect and blame
- Contributors must rebase before push
- Occasional conflict resolution cost accepted

---

## ADR-004: Dependency direction

**Status:** Accepted  
**Date:** 2026-07-20

### Context

Configuration loading and diagnostics formatting must not create cycles with the
hot runtime.

### Decision

Enforce:

```
Abstractions ← Runtime ← Configuration ← Sample
Abstractions ← Diagnostics
```

Sample may reference Diagnostics. Runtime must not reference Configuration,
Diagnostics, or Sample. Architecture tests enforce references.

### Consequences

- Clear layering
- Diagnostics consumes Abstractions event shapes / ids only
- Configuration can depend on Runtime types for builders without Runtime
  depending back

---

## ADR-005: Symbolic 64-bit world facts

**Status:** Accepted  
**Date:** 2026-07-20

### Context

Planners need compact state. Continuous combat values belong in host/memory.

### Decision

Represent world state as ≤ 64 boolean `WorldFactId` bits; quantize continuous
inputs before planning.

### Consequences

- Fast masks and hashing for closed set
- Fact vocabulary changes require discipline
- Rich beliefs live in working memory, not the mask

---

## ADR-006: Freeze barrier and Power-of-Ten adaptation

**Status:** Accepted  
**Date:** 2026-07-20

### Context

Gameplay AI must be predictable under load; GC spikes are unacceptable on the
tick path.

### Decision

Allocate during configuration/init; after `Freeze()`, `[FrozenRuntimePath]` code
obeys Power-of-Ten C# adaptations; enforce via Audit tool and build settings
(`TreatWarningsAsErrors`, no unsafe, overflow checks).

### Consequences

- More verbose buffer management
- Stronger CI gate once Audit matures
- Sample/tests remain free to allocate

---

## ADR-007: Engine-agnostic host services

**Status:** Accepted  
**Date:** 2026-07-20

### Context

Binding the core to a single engine reduces reuse and complicates headless CI.

### Decision

Keep Runtime free of engine types; integrate through host service interfaces and
tick pumping ([ENGINE_INTEGRATION_GUIDE.md](ENGINE_INTEGRATION_GUIDE.md)).

### Consequences

- Sample console proves the loop without Unity/Unreal
- Each engine needs an adapter
- Navigation is always external (`REQ-NAV-001`)

---

## ADR-008: Explicit failure enums over exceptions

**Status:** Accepted  
**Date:** 2026-07-20

### Context

Combat contingencies (no path, cover lost) are expected.

### Decision

Use `OperationStatus`, `PlannerStatus`, and `ActionFailureReason` for expected
failures; reserve exceptions for true programmer errors outside tick policy.

### Consequences

- Call sites must check statuses (Rule 7)
- Richer diagnostics and deterministic recovery


## ADR-009 — CA1515 handling for test and executable projects

**Date:** Project sequence after TG-DIA-001 / TG-SIM-001

**Context:** With `AnalysisLevel=latest-all` and `TreatWarningsAsErrors`, CA1515
requires non-entry types in application projects to be `internal`. Test helpers
and console entry assemblies trigger noise without improving the library API.

**Decision:**
- Test projects (`IsTestProject=true`) suppress CA1515 via `Directory.Build.props`.
- Sample and Audit executables may suppress CA1515/CA1303 locally for entry-point
  and console-facing types.
- Library projects (`Abstractions`, `Runtime`, `Configuration`, `Diagnostics`) do
  **not** suppress CA1515; public surface remains intentional.

**Alternatives:** Make every test type internal (done for AllocationProbe);
disable CA1515 globally (rejected).

**Consequences:** Aligns with "no blanket NoWarn" for production libraries while
keeping executables and tests buildable.
