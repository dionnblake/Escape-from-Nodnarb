# Visual Overnight Log

Reference slice: Crash Basin, campaign Level 2. Captures are deterministic 720x1280 editor renders under `Artifacts/VisualChecks/`.

## Checkpoint 1
- Problem: Crash Basin relied on primitive-looking world geometry and lacked repeatable visual evidence.
- Hypothesis: A small authored low-poly kit plus a fixed capture harness would establish an intentional visual base.
- Changed: Generated/imported 15 Crash Basin FBX assets; added deterministic Level 2 placement; extended stylized shader; added `Editor/VisualCaptureHarness.cs`; documented provenance.
- Validation: Blender compile/export pass; provenance pass; Unity EditMode pass; capture exit 0; opening/combat/heavy PNGs created and inspected.
- Evidence: `Artifacts/VisualChecks/crash-basin-opening.png`, `crash-basin-combat.png`, `crash-basin-heavy.png`.
- Result: Authored canyon silhouettes, wreckage, rocks, cliffs, flora, and repeatable portrait inspection established.
- Remaining weakness: Ground still read as repeated slabs; lighting was too shadow-heavy.
- Commit: `0579cf4`.

## Checkpoint 2
- Problem: Visible lane was dark and slab-dominant.
- Hypothesis: A continuous floor bed, smaller authored chunks, and Level 2-only material contrast would improve floor continuity and subject separation.
- Changed: Added Crash Basin floor bed/fractures; applied Level 2 `MaterialPropertyBlock` ambient, directional, shadow, and rim treatment; aligned capture lighting/shader selection.
- Validation: Unity EditMode 70/70; Unity PlayMode 56/56; capture pass; opening/combat/heavy renders inspected.
- Evidence: `Artifacts/VisualChecks/capture-contrast-pass.log` and PNGs.
- Result: Warm terrain, charcoal shadows, cool rim, and fog became more readable.
- Remaining weakness: No strong distant destination marker.
- Commit: `d6f2970`.

## Checkpoint 3
- Problem: Background lacked a readable authored focal point.
- Hypothesis: Existing Crash Basin spire assets plus a restrained cyan signal mast would create destination composition without new dependencies.
- Changed: Added and tuned the Level 2 signal beacon composition.
- Validation: Unity PlayMode 56/56; capture pass with no compiler errors or exceptions; opening/heavy renders inspected.
- Evidence: `Artifacts/VisualChecks/capture-beacon-refined.log` and PNGs.
- Result: Beacon reads at portrait scale without obscuring combat.
- Remaining weakness: Ground islands remained too regularly centered.
- Commit: `577f9fb`.

## Checkpoint 4
- Problem: Ten centered ground islands still read as modular slabs.
- Hypothesis: Keep the same deterministic budget, but shrink and laterally stagger islands over the continuous bed.
- Changed: Side-biased deterministic placement and smaller authored ground-island scale ranges.
- Validation: Capture pass with no compiler errors or exceptions; opening/heavy renders inspected; Unity PlayMode 56/56.
- Evidence: `Artifacts/VisualChecks/capture-ground-islands.log`; `Artifacts/TestResults/visual-ground-islands-playmode.xml`.
- Result: Lane now reads as continuous canyon floor with broken crust rather than a centered slab chain.
- Remaining weakness: The visual language is still very dark and the gameplay subjects are static in editor captures.
- Commit: `237c050`.

## Checkpoint 5
- Problem: The distant ridge forms sat at the fog boundary and contributed little background silhouette.
- Hypothesis: Move the existing five ridge assets into the readable fog band and slightly increase their scale to create layered canyon depth.
- Changed: Level 2-only ridge placement moved from z≈26.4 to z≈23.4 with bounded scale increase; deterministic route and gameplay geometry unchanged.
- Validation: Unity capture pass with no compiler errors or exceptions; opening/heavy renders inspected; Unity PlayMode 56/56.
- Evidence: `Artifacts/VisualChecks/capture-ridge-depth.log`; final accepted render `Artifacts/VisualChecks/capture-final-accepted.log`.
- Result: Background silhouettes now frame the beacon and create a clearer distant layer without crossing combat subjects.
- Remaining weakness: Captain/crew presentation still reads as flat prototype material in the editor harness; combat motion/VFX and device performance remain unverified.
- Commit: `4b253e8`.

## Rejected experiments
- Mid-background flora placement: rejected because both placements were outside the useful portrait read and produced no visible authored improvement.
- Beacon pulse repositioning: rejected because the pulse/crossbar either stayed hidden or clipped the top frame.
- Bioluminescent floor veins: rejected because the visible result was too faint to count and was removed. The original seeded layout was preserved with an independent-seed check before removal.

## Stop condition
- Stopped at five validated visual checkpoints because the next high-impact improvement is character/material authoredness and combat feedback, which requires owner art direction or a broader asset/presentation decision rather than another safe Crash Basin placement tweak.

## Current gates
- EditMode: 70/70 passed. Artifact: `Artifacts/TestResults/visual-final-editmode.xml`.
- PlayMode: 56/56 passed. Artifact: `Artifacts/TestResults/visual-final-playmode.xml`.
- Final accepted capture: pass; zero compiler errors; zero exceptions. Artifact: `Artifacts/VisualChecks/capture-final-accepted.log`.
- Android: blocked, Unity Android module unavailable.
- Device/performance profiling: unverified, no `adb`/device evidence.
- Generic context verifier: UNVERIFIED because no recognized package.json, Cargo.toml, or pyproject.toml exists.
- Release readiness: not claimed.
