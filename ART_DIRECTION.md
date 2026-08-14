# Escape from Nodnarb visual direction

## Purpose

This document is the approved visual contract for the first Android vertical slice. It describes the presentation layer only. Gameplay rules, stage timing, route geometry, spawn data, save formats, input, and progression remain owned by the existing runtime contracts.

## Direction

- Stylized low-poly alien survival fiction: a crashed salvage crew moving through unfamiliar living terrain while waiting for rescue.
- Strong portrait silhouettes. The Captain is the nearest readable focal unit; the crew trails behind; the horde owns the far field.
- Dark, matte, industrial survival gear against organic alien greens, violet growth, rescue green signals, and restrained orange weapon light.
- Wilderness framing only: shelves, growth, pods, exposed rock, wreckage, nests, switchbacks, and broken natural passages. No roads, lane plates, railings, or civilized architecture.
- One low-resolution shadow-casting key light, non-shadow fill, linear fog, and mobile-safe materials. Depth must come from shape, value grouping, rim light, and authored landmarks, not post-processing.
- Combat feedback is brief and directional: muzzle flash, readable bolt, impact pulse, enemy hit/death response, and card pickup confirmation. Effects are pooled and disabled or simplified by reduced-motion settings.

## Stage 3 slice

The first art pass targets the opening 45 seconds of `N-03 Unknown March`:

- biome: `SporeField`
- route: `UnknownWinding`
- seed: `3301`
- encounter identity: crossfire pressure with the Captain, CrewSoldier, and Rusher silhouettes
- landmark identity: an authored `SporeArch` passage plus the existing `HiveGrowth` landmark

The same stage seed, route, spawn timing, card windows, and combat budget must be used for before/after captures.

## Mobile budgets

- 60 FPS target: 16.67 ms per gameplay frame.
- Main thread and render thread: 8 ms p95 each; GPU: 12 ms p95.
- 120 visible batches target, 160 hard ceiling; 80k visible triangles hard ceiling.
- Two real-time lights maximum, one shadow caster, no realtime reflections or post stack.
- No unbounded pool growth or steady-state allocation churn after warm-up.
- Visual asset memory increase: 64 MB maximum over the current baseline; gameplay textures normally stay at 1024 or below with explicit Android compression settings.

## Replacement rules

- Replace presentation children under existing runtime roots first.
- Preserve resource names, root transforms, `MuzzlePoint`, actor bounds, route anchors, timing, and pooled object state.
- Do not delete a placeholder until the replacement is imported, captured, tested, and has a recorded provenance row in `LICENSES.md`.
- New art must be original or project-generated. The Hero Wars reference remains read-only and is never packaged.

## Approval gate

This direction is approved for local implementation only. Store release, public claims, external uploads, monetization, rights approval, and iOS verification remain open owner-controlled gates.
