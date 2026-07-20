# Target and Weapon Selection

Before planning, each agent refreshes **focus target** and **selected weapon**.
These subsystems write world facts consumed by goals and action candidates.

Related: [PERCEPTION_AND_MEMORY.md](PERCEPTION_AND_MEMORY.md),
[WORLD_STATE_MODEL.md](WORLD_STATE_MODEL.md),
[ACTION_EXECUTION.md](ACTION_EXECUTION.md).

## Target selection

### Inputs

- Working-memory records of type `TargetSeen`, `TargetHeard`, damage sources
- Squad shared contact via communication intents
- Existing focus (hysteresis)

### Outputs

| Fact | Condition |
|------|-----------|
| `TargetSelected` | A focus `EntityId` is assigned |
| `TargetKnown` | Evidence exists in memory |
| `TargetVisible` | Fresh visual confirmation |
| `TargetAlive` | Belief from last evidence / host query |

### Scoring (deterministic)

Candidates are scored with integer weights from configuration, for example:

- Visible contact ≫ heard contact
- Closer distance band (quantized) scores higher
- Damage dealer bias
- Sticky focus bonus to reduce thrashing

Ties break on ascending `EntityId`. Maximum candidates considered per tick align
with perception caps.

### Target loss

When focus evidence expires: set `TargetLost` memory, clear `TargetVisible`,
possibly clear `TargetSelected`. Running actions fail with
`ActionFailureReason.TargetLost` or `TargetDead`.

## Weapon selection

### Inputs

- Inventory / ammo reported by host
- Range band to focus
- Goal hints (suppress vs. melee vs. grenade)

### Outputs

| Fact | Condition |
|------|-----------|
| `WeaponSelected` | `WeaponId` assigned |
| `WeaponLoaded` | Magazine ready |
| `HasAmmunition` | Ammo remaining |
| `HasGrenade` | Throwable available |
| `MeleeRangeAvailable` | Distance band allows melee |
| `BlindFireAppropriate` | Cover geometry + threat |

Selection prefers weapons whose range band matches the focus. Changing weapons
mid-plan may invalidate candidates; executor treats host rejection as
`WeaponRejected`.

## Interaction with planning

Action candidate generation binds the current `EntityId` / `WeaponId` into
attack, suppress, and reload candidates so the planner does not search unbound
parameters.

## Implementation status

Identifiers and facts: **Implemented**. Selectors: **Pending**.
