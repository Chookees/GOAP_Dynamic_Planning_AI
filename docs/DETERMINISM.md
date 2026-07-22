# Determinism

DynamicPlanningAI is designed so that **identical inputs and tick sequences produce
identical AI decisions**. This enables replay, lockstep tests, and simulation
regression.

Related: [ARCHITECTURE.md](ARCHITECTURE.md),
[MEMORY_AND_ALLOCATIONS.md](MEMORY_AND_ALLOCATIONS.md),
[TESTING.md](TESTING.md),
[PERFORMANCE.md](PERFORMANCE.md).

## Time source

- Sole clock: host-provided `AiTick(sequence, deltaMilliseconds)`.
- `Sequence` must be monotonic non-decreasing across calls.
- `DeltaMilliseconds` must be positive and ≤ `MaximumTickDeltaMilliseconds`
  (250).
- Runtime must not call `DateTime`, `Stopwatch`, or environment clocks on the
  AI path.

## Ordering rules

1. Process agents in ascending `AgentId`.
2. Process squads in ascending `SquadId`.
3. Sort ties in heaps / arbitration by explicit secondary keys (ids), never by
   hashcode randomization.
4. Drain ingress buffers in index order.
5. Do not depend on `Dictionary` / `HashSet` enumeration order for decisions;
   use dense arrays and integer keys.

## Randomness

If a scenario needs stochastic choice, use a **host-injected deterministic RNG**
seeded in configuration and advanced only through recorded draws. Default sample
scenarios should prefer pure deterministic scoring (no RNG).

## Floating point

Symbolic planning uses integers. Where hosts supply floats (distances, angles),
quantizers convert to integer bands before scoring. Document band edges in
configuration. Avoid cross-platform float nondeterminism in core decisions.

## Threading

`AiRuntime` assumes **single-threaded** tick entry. Hosts must not call tick
concurrently. Parallelism, if any, stays inside host services and must complete
before the next tick observes results.

## Replay recipe

1. Record seed, config hash, and every `AiTick`.
2. Record host service responses (path results, hit/miss).
3. Replay offline in Sample / SimulationTests and compare diagnostic digests.

## Requirement

`REQ-DET-001` — see [REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md).
Abstractions (`AiTick`) **Implemented**; full runtime guarantees **Pending**
until Runtime lands.
