# Perception and Memory

Sensors do not feed the planner directly. They write **working-memory records**;
quantizers then derive `WorldFactId` bits. This separation keeps planning symbolic
and lets stale evidence expire without rewriting the planner.

Related: [WORLD_STATE_MODEL.md](WORLD_STATE_MODEL.md),
[TARGET_AND_WEAPON_SELECTION.md](TARGET_AND_WEAPON_SELECTION.md),
[COMMUNICATION_SYSTEM.md](COMMUNICATION_SYSTEM.md),
[DETERMINISM.md](DETERMINISM.md).

## Ingress buffers (per tick)

The host pushes events into fixed buffers before or during the tick:

| Buffer | Cap (`AiHardLimits`) |
|--------|----------------------:|
| Perception candidates | 32 |
| Sound events | 32 |
| Damage events | 16 |
| Danger events | 16 |

Overflow returns `OperationStatus.CapacityExceeded` and drops newest or oldest
per configured policy (documented in scenario config). Drops are recorded in
diagnostics.

## Working memory

- Cap: `MaximumMemoryRecords` = 64 per agent.
- Records identified by `MemoryRecordId`.
- Typed by `MemoryType`.

### MemoryType catalog

| Type | Typical source |
|------|----------------|
| `TargetSeen` | Visual perception |
| `TargetHeard` | Sound events |
| `DamageReceived` | Damage ingress |
| `DangerDetected` / `GrenadeDetected` | Danger ingress |
| `CoverInvalid` / `CoverReserved` / `CoverReservationLost` | Cover system |
| `DoorBlocked` / `DoorBreached` | Interaction |
| `TraversalFailed` / `NavigationFailed` | Nav / traversal |
| `SearchSectorInspected` | Search actions |
| `SquadOrderReceived` / `Completed` / `Failed` | Squad |
| `TargetLost` | Perception loss |
| `CommunicationReceived` | Communication system |

Each record stores: type, subject entity/point ids, tick stamp, confidence or
TTL fields as plain integers (no heap objects on frozen path).

## Update algorithm

1. Drain ingress in deterministic order (buffer index order).
2. Upsert or insert memory records; if full, evict lowest priority / oldest by
   policy.
3. Expire records whose TTL elapsed relative to `AiTick.Sequence`.
4. Run quantizers to set/clear facts (`TargetKnown`, `TargetVisible`,
   `UnderDirectFire`, etc.).

## Belief vs. truth

Memory is the agent’s **belief**. The host may know a target moved; until a
sensor or ally communication updates memory, the agent may still plan with
`TargetKnown` and without `TargetVisible`.

## Implementation status

`MemoryType` and limits: **Implemented**. Memory store and quantizers:
**Pending** (`REQ-GOAP-001`).
