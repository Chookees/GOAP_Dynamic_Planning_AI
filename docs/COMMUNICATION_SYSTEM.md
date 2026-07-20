# Communication System

Agents exchange **semantic intents**, not free-form chat. Intents update ally
working memory and can bias goal arbitration (e.g. shared contact).

Related: [PERCEPTION_AND_MEMORY.md](PERCEPTION_AND_MEMORY.md),
[SQUAD_COORDINATION.md](SQUAD_COORDINATION.md),
[GOAL_ARBITRATION.md](GOAL_ARBITRATION.md).

## Intent types (`CommunicationIntentType`)

| Intent | Purpose |
|--------|---------|
| `ContactSpotted` / `ContactLost` | Shared awareness |
| `TakingFire` | Request support / cover |
| `CoverCompromised` / `MovingToCover` | Cover status |
| `Advancing` / `Suppressing` | Maneuver calls |
| `Reloading` | Temporary vulnerability |
| `ThrowingGrenade` / `GrenadeWarning` | Lethal area denial |
| `SearchingSector` / `SectorClear` | Search progress |
| `DoorBlocked` / `BreachingDoor` | Obstacle status |
| `NoValidRoute` | Nav failure broadcast |
| `HoldingPosition` | Hold ack |
| `NeedAssistance` | Help request |
| `OrderAcknowledged` / `OrderFailed` | Squad protocol |

Identified by `CommunicationIntentId`. Pending requests capped at
`MaximumCommunicationRequests` (32).

## Arbitration

Not every intent is spoken every tick. A communication arbitrator:

1. Collects pending requests from agents.
2. Scores by urgency and squad relevance.
3. Emits a bounded number of intents per tick.
4. Delivers to recipients in range / same squad (host provides range query or
   uses squad membership only).

Delivery writes `MemoryType.CommunicationReceived` and may upsert target
evidence (e.g. contact position belief).

## Separation from audio

Voice / bark presentation is a **host concern**. The runtime emits semantic
intents; the engine may map them to audio, UI markers, or nothing.

## Determinism

Emission order is sorted by score then id. Delivery order follows recipient
`AgentId` order.

## Implementation status

Intent enums and limits: **Implemented**. Arbitrator: **Pending**.
