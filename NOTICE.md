# NOTICE

## DynamicPlanningAI (DP_AI)

**DP_AI** is the short form of **DynamicPlanningAI**. **DP** means **DynamicPlanning**.

Copyright (c) 2026 DynamicPlanningAI Contributors

DynamicPlanningAI (DP_AI) is an **original**, engine-agnostic Goal-Oriented Action Planning (GOAP)
framework written in C# / .NET. All source code, configuration schemas, sample
scenarios, tests, and documentation in this repository are original works of the
DynamicPlanningAI contributors unless otherwise noted below.

## Inspiration (not derivation)

DynamicPlanningAI is **inspired by publicly documented principles** of tactical AI and
GOAP systems discussed in industry talks, postmortems, and academic literature
associated with titles such as F.E.A.R. Those materials describe high-level ideas
such as:

- Goal-oriented action planning with symbolic world state
- Working memory for sensed entities and events
- Goal arbitration and plan reuse under time pressure
- Squad-level coordination layered on individual planners

**DynamicPlanningAI is not a reverse-engineered or proprietary reproduction** of any
commercial game engine, AI DLL, or closed-source asset. No proprietary source,
binaries, assets, or confidential materials were used. Algorithmic choices,
data layouts, APIs, capacity limits, and naming are original engineering decisions
documented in [docs/DESIGN_DECISIONS.md](docs/DESIGN_DECISIONS.md) and
[docs/PUBLIC_SOURCE_FIDELITY.md](docs/PUBLIC_SOURCE_FIDELITY.md).

## Third-party software

This repository may depend on the following third-party components (versions are
pinned in `Directory.Packages.props`):

| Component | License (typical) | Role |
|-----------|-------------------|------|
| .NET / BCL | MIT | Runtime platform |
| System.Text.Json | MIT | Configuration serialization |
| Microsoft.CodeAnalysis.CSharp | MIT | Audit tool Roslyn analysis |
| xUnit / Microsoft.NET.Test.Sdk / coverlet | Apache-2.0 / MIT | Test infrastructure |

Consult each package’s own license file for authoritative terms. No GPL or other
copyleft dependency is intentionally introduced into production assemblies.

## Trademarks

Product names, company names, and trademarks mentioned in documentation for
historical or educational context remain the property of their respective owners.
Use of those names does not imply endorsement or affiliation.
