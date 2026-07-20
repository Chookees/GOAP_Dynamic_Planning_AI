# Memory and Allocations

Production AI ticks run under a **freeze barrier**: after configuration and
buffer allocation, the frozen runtime path must not deliberately allocate
managed objects.

Related: [POWER_OF_TEN_COMPLIANCE.md](POWER_OF_TEN_COMPLIANCE.md),
[ARCHITECTURE.md](ARCHITECTURE.md),
[PERFORMANCE.md](PERFORMANCE.md),
[CONFIGURATION.md](CONFIGURATION.md).

## Lifecycle and allocation windows

| State | Allocations |
|-------|-------------|
| Created / Configuring / Initialized | Allowed (build tables) |
| Frozen / Running | Forbidden on `[FrozenRuntimePath]` |
| Stopped | Reset counters; no growth |
| Disposed | Release |

Attribute: `FrozenRuntimePathAttribute` marks methods/types audited more
strictly.

## Capacity-first design

All major structures are fixed at freeze using values ≤ `AiHardLimits`:

- Agent / squad tables
- Planner node pool, open-set heap storage, closed-set slots
- Memory records, perception buffers
- Tactical points, reservations
- Diagnostic ring buffer (`MaximumDiagnosticRecords` = 4096)

Growth APIs return `OperationStatus.CapacityExceeded` instead of resizing.

## REQ-ALLOC-001 policy

1. No `new` reference types on frozen path.
2. No LINQ, string interpolation, or boxing in hot loops.
3. Prefer `readonly struct` identifiers and status enums (`byte` where applicable).
4. Use preallocated `Span<T>` / arrays for temporary scratch owned by the agent
   workspace.
5. Diagnostic **formatting** for humans may allocate only when the host requests
   a dump off the tick path.

## Checked arithmetic

`CheckForOverflowUnderflow` is enabled solution-wide. Cost accumulation uses
checked math; overflow maps to `PlannerStatus.CostOverflow` /
`OperationStatus.ArithmeticOverflow`.

## Implementation status

Limits and attribute: **Implemented**. Runtime pools and audit enforcement:
**PartiallyImplemented / Pending** (`REQ-ALLOC-001`, `REQ-PO10-001`).
