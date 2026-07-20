# Power of Ten Compliance

TacticalGoap adapts NASA / JPL “Power of Ten” rules for safety-critical software
to a **C# tactical AI runtime**. The goal is predictable control flow, bounded
resources, and auditable critical paths—not literal embedded C.

Related: [MEMORY_AND_ALLOCATIONS.md](MEMORY_AND_ALLOCATIONS.md),
[TESTING.md](TESTING.md), [DESIGN_DECISIONS.md](DESIGN_DECISIONS.md),
`AiHardLimits.MaximumMethodLogicalLines`.

## Rule map

| # | Classic rule (summary) | C# adaptation | Enforcement |
|---|------------------------|---------------|-------------|
| 1 | Restrict control flow complexity | No recursion on frozen path; prefer early returns; ban `goto` | Audit + code review |
| 2 | Bound all loops | Every loop has a compile-time or config-validated upper bound | Audit + unit tests |
| 3 | No dynamic memory after init | No deliberate managed allocation on `[FrozenRuntimePath]` after `Freeze()` | Audit + lifecycle tests |
| 4 | Short functions | Method logical lines ≤ `MaximumMethodLogicalLines` (60) | Audit tool |
| 5 | Assertion density | Contracts / `OperationStatus` checks at subsystem boundaries | Diagnostics + tests |
| 6 | Minimize data scope | Prefer `readonly struct`, file-local helpers, no public mutable statics | Style + architecture tests |
| 7 | Check return values | Non-void results must be inspected; no ignored `PlannerResult` | Analyzers + review |
| 8 | Limit preprocessor use | No `#if` feature forks in Runtime critical path | Audit |
| 9 | Limit pointer use | `AllowUnsafeBlocks=false`; no unsafe pointers | Directory.Build.props |
| 10 | Compile with all warnings | `TreatWarningsAsErrors`, `AnalysisLevel=latest-all`, XML docs on production | Build |

## Rule details

### Rule 1 — Simple control flow

**Adaptation:** Frozen-path methods must not recurse. Indirect recursion through
planner expansion is replaced by an explicit open set and expansion counter.
Exceptions are not used for expected AI failures; use `OperationStatus`,
`PlannerStatus`, and `ActionFailureReason`.

**Example (compliant):** expand nodes in a `for` loop capped by
`MaximumPlannerExpansionsPerStep`.

**Limitation:** Host adapters outside the frozen path may use normal C# patterns.

### Rule 2 — Bounded loops

**Adaptation:** Loop upper bounds come from `AiHardLimits` or validated
configuration integers. Searching unbounded collections (LINQ over growing lists)
is forbidden on the frozen path.

**Example:**

```csharp
for (int i = 0; i < candidateCount && i < AiHardLimits.MaximumActionCandidates; i++)
{
    // ...
}
```

### Rule 3 — No dynamic allocation after freeze

**Adaptation:** Allocate fixed arrays/tables during `Configuring` / `Initialized`.
After `Frozen`, methods marked `[FrozenRuntimePath]` must not call APIs that
allocate (string formatting, LINQ, `new` reference types, resizing collections).

**Limitation:** Diagnostic **formatting** for human logs may allocate off the
frozen path when the host explicitly requests a dump.

### Rule 4 — Short functions

**Adaptation:** Logical line count ≤ 60 (`AiHardLimits.MaximumMethodLogicalLines`).
Decompose planner expand / goal score / cover score into helpers.

### Rule 5 — Assertions and contracts

**Adaptation:** Prefer explicit status returns. Diagnostics may assert invariants
in debug builds. Contract violations map to `OperationStatus.ContractViolation`.

### Rule 6 — Small data scope

**Adaptation:** Identifiers are readonly structs. World state is a value bitmask.
Avoid mutable static caches. Implicit usings are disabled project-wide.

### Rule 7 — Check returns

**Adaptation:** Callers inspect `PlannerResult.Status` before reading plan length
or cost. Architecture tests and review catch discarded results.

### Rule 8 — Minimal conditional compilation

**Adaptation:** Behavior differences go through configuration flags validated at
load time, not `#if UNITY` forks inside Runtime.

### Rule 9 — No unsafe code

**Adaptation:** `AllowUnsafeBlocks=false`. Spans over preallocated arrays are
allowed when they do not escape or allocate.

### Rule 10 — Full warning / analyzer bar

**Adaptation:** `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`,
`CheckForOverflowUnderflow`, production XML documentation (`CS1591` as error).

## Audit tool

```bash
dotnet run --project tools/TacticalGoap.Audit --configuration Release
```

Expected checks (as the audit tool matures):

- `[FrozenRuntimePath]` methods for allocation APIs
- Method logical line counts
- Unbounded `while (true)` without counters
- `unsafe` / `#if` in Runtime
- Recursion on frozen types

Until the audit implementation is complete, treat these as review requirements.
Status: **PartiallyImplemented** (`REQ-PO10-001`).

## Non-goals

- Not MISRA C or DO-178 certification
- Not a ban on all modern C# features (records, pattern matching are allowed off
  the frozen path when they do not allocate unexpectedly)
- Sample and test projects may allocate freely
