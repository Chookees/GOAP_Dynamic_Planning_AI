# World State Model

Planning operates on a **symbolic bit-mask** of world facts. Continuous host
quantities (distance, ammo count, angles) are quantized into these bits before
goal arbitration and planning.

Related: [GOAP_PLANNER.md](GOAP_PLANNER.md),
[PERCEPTION_AND_MEMORY.md](PERCEPTION_AND_MEMORY.md),
[ACTION_EXECUTION.md](ACTION_EXECUTION.md),
[DETERMINISM.md](DETERMINISM.md).

## Representation

- Facts are identified by `WorldFactId` (`byte` enum).
- Hard width: `AiHardLimits.MaximumWorldFacts` = **64**.
- Runtime stores an agent’s current mask as a 64-bit value (or fixed bitset).
- Facts are boolean only: **no object references, floats, or strings** in the
  symbolic state.

Action **preconditions** and **effects** are masks (require-true, require-false,
set-true, set-false) applied during regression and, after successful execution,
during commit.

## Fact catalog and ownership

Ownership answers: which subsystem is allowed to set or clear the bit.

| Fact | Typical owner | Notes |
|------|---------------|-------|
| `TargetSelected` | Target selection | Focus entity present |
| `TargetKnown` | Memory / perception | Evidence record exists |
| `TargetVisible` | Perception quantizer | LOS confirmed this tick |
| `TargetAlive` | Host combat / memory | Belief; may lag truth |
| `AtTacticalPoint` | Navigation / execution | Occupying a point |
| `InCover` | Cover / posture | Valid cover posture |
| `CoverValid` | Cover evaluation | Point still scores |
| `WeaponSelected` | Weapon selection | Loadout focus |
| `WeaponLoaded` | Weapon / reload action | Magazine ready |
| `HasAmmunition` | Weapon / inventory host | Ammo remaining |
| `HasGrenade` | Inventory host | Throwable available |
| `UnderDirectFire` | Damage / danger ingress | Suppression pressure |
| `GrenadeDangerPresent` | Danger events | Flee / dodge goals |
| `MovementDestinationSet` | Navigation orchestration | Destination reserved |
| `AtMovementDestination` | Navigation follow | Arrival |
| `DoorKnown` | Perception / smart objects | Relevant door |
| `DoorOpen` | Interaction / host | Traversal clear |
| `DoorBlocked` | Interaction failure | Memory + fact |
| `TraversalAvailable` | Nav links | Window/vault/etc. |
| `SquadOrderAvailable` | Squad coordinator | Pending order |
| `SquadOrderSatisfied` | Execution / squad | Order complete |
| `SearchLocationAvailable` | Search / squad | Sector assigned |
| `SearchLocationInspected` | Search action | Sector cleared |
| `ImmediateDangerResolved` | Danger goals | Post-flee |
| `AtWindowTraversal` | Traversal actions | At window point |
| `WindowTraversed` | Traversal actions | Completed traverse |
| `Suppressing` | Suppression action | Active suppress |
| `BlindFireAppropriate` | Cover + threat geometry | Quantized |
| `MeleeRangeAvailable` | Distance quantizer | Close combat |
| `ImmediateDangerPresent` | Danger ingress | High priority |
| `PathValid` | Navigation | Current path OK |
| `ReadinessMaintained` | Idle / patrol goals | Standby |

Unused enum slots remain reserved for future facts without widening the mask
beyond 64 without an ADR.

## Quantization rules

1. Quantizers run after perception/memory updates and before arbitration.
2. Thresholds are configuration constants frozen at init (no runtime mutation).
3. Quantization must be **deterministic** given the same host inputs and tick
   ([DETERMINISM.md](DETERMINISM.md)).
4. When evidence is uncertain (stale memory), prefer conservative facts (e.g.
   clear `TargetVisible`, keep `TargetKnown`).

## Action effects vs. volatile truth

Planner effects are **predicted**. Execution re-validates volatile preconditions
each tick. If the host reports ammo empty, clear `HasAmmunition` and fail with
`ActionFailureReason.NoAmmunition` rather than leaving a stale true bit.

## Multi-agent isolation

Each agent has its own world-state mask. Shared environment truth (door open,
cover reserved) is reflected per agent through perception, communication, or
reservation queries—not a single global planner state.

## Implementation status

`WorldFactId` catalog: **Implemented**. Mask helpers and quantizers:
**Pending / PartiallyImplemented** (`REQ-GOAP-001`).
